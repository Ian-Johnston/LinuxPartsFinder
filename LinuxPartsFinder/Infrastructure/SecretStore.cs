using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace LinuxPartsFinder;

/// <summary>
/// Where API keys are kept - never in settings.json. Each person's keys stay on their own computer:
/// the desktop keyring on Linux (or, without one, a file only this user can read), DPAPI-encrypted (current
/// Windows user only) on Windows.
/// </summary>
public interface ISecretStore
{
    /// <summary>The saved value, or "" if there isn't one (or it can't be read).</summary>
    string Get(string name);

    /// <summary>Saves the value; "" removes it.</summary>
    void Set(string name, string value);
}

public static class SecretStore
{
    public static ISecretStore ForThisPlatform(string settingsFolder)
    {
        if (OperatingSystem.IsLinux())
            return (ISecretStore?)LinuxKeyringStore.TryCreate() ?? new LinuxFileStore(Path.Combine(settingsFolder, "keys.json"));
        if (OperatingSystem.IsWindows()) return new WindowsDpapiStore(Path.Combine(settingsFolder, "keys.json"));
        return new NoSecretStore();
    }
}

/// <summary>
/// Linux desktop keyring (GNOME Keyring, KDE Wallet, KeePassXC... - anything offering the Secret Service), through
/// libsecret. One "LinuxPartsFinder - name" password per key, visible in Passwords and Keys (Seahorse) or KWalletManager.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxKeyringStore : ISecretStore
{
    private const string SecretLib = "libsecret-1.so.0";
    private const string GLib = "libglib-2.0.so.0";
    private const string SchemaName = "com.ianjohnston.LinuxPartsFinder";
    private const string KeyAttribute = "key";

    private readonly IntPtr _schema = MakeSchema();

    /// <summary>The keyring store, or null if libsecret isn't installed or no keyring service is running.</summary>
    public static LinuxKeyringStore? TryCreate()
    {
        try
        {
            var store = new LinuxKeyringStore();
            store.Lookup("probe"); // throws if libsecret or the keyring service is missing
            return store;
        }
        catch
        {
            return null;
        }
    }

    public string Get(string name)
    {
        try
        {
            return Lookup(name);
        }
        catch
        {
            return ""; // keyring locked and the unlock prompt cancelled, or access refused
        }
    }

    public void Set(string name, string value)
    {
        using var attributes = new Attributes(name);
        IntPtr error;
        if (string.IsNullOrEmpty(value))
            secret_password_clearv_sync(_schema, attributes.Table, IntPtr.Zero, out error); // false if there was nothing to remove
        else
            secret_password_storev_sync(_schema, attributes.Table, IntPtr.Zero, "LinuxPartsFinder - " + name, value, IntPtr.Zero, out error);
        var message = TakeError(error);
        if (message != null) throw new InvalidOperationException($"Could not save {name} in the keyring ({message}).");
    }

    private string Lookup(string name)
    {
        using var attributes = new Attributes(name);
        var password = secret_password_lookupv_sync(_schema, attributes.Table, IntPtr.Zero, out var error);
        var message = TakeError(error);
        if (message != null) throw new InvalidOperationException(message);
        if (password == IntPtr.Zero) return ""; // not saved yet
        try
        {
            return Marshal.PtrToStringUTF8(password) ?? "";
        }
        finally
        {
            secret_password_free(password);
        }
    }

    /// <summary>The GError's message (then frees it), or null if there wasn't one.</summary>
    private static string? TakeError(IntPtr error)
    {
        if (error == IntPtr.Zero) return null;
        var message = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(error, 8)) ?? "unknown error"; // GError: domain, code, message
        g_error_free(error);
        return message;
    }

    /// <summary>
    /// SecretSchema: name, flags, 32 x {attribute name, type}, then reserved fields - all zero except the name and the one
    /// "key" attribute (type 0 = string). Kept for the life of the app, as libsecret expects.
    /// </summary>
    private static IntPtr MakeSchema()
    {
        var p = IntPtr.Size;
        var size = p * 2 + 32 * p * 2 + p * 8;
        var schema = Marshal.AllocHGlobal(size);
        Marshal.Copy(new byte[size], 0, schema, size);
        Marshal.WriteIntPtr(schema, 0, Marshal.StringToCoTaskMemUTF8(SchemaName));
        Marshal.WriteIntPtr(schema, p * 2, Marshal.StringToCoTaskMemUTF8(KeyAttribute));
        return schema;
    }

    /// <summary>The attributes a key is found by - a GHashTable of { "key": name }.</summary>
    private sealed class Attributes : IDisposable
    {
        private static readonly IntPtr GLibHandle = NativeLibrary.Load(GLib);
        private readonly IntPtr _key = Marshal.StringToCoTaskMemUTF8(KeyAttribute);
        private readonly IntPtr _value;

        public IntPtr Table { get; }

        public Attributes(string name)
        {
            _value = Marshal.StringToCoTaskMemUTF8(name);
            Table = g_hash_table_new(NativeLibrary.GetExport(GLibHandle, "g_str_hash"), NativeLibrary.GetExport(GLibHandle, "g_str_equal"));
            g_hash_table_insert(Table, _key, _value);
        }

        public void Dispose()
        {
            g_hash_table_unref(Table);
            Marshal.FreeCoTaskMem(_key);
            Marshal.FreeCoTaskMem(_value);
        }
    }

    [DllImport(SecretLib)]
    private static extern IntPtr secret_password_lookupv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [DllImport(SecretLib)]
    private static extern int secret_password_storev_sync(IntPtr schema, IntPtr attributes, IntPtr collection,
                                                          [MarshalAs(UnmanagedType.LPUTF8Str)] string label,
                                                          [MarshalAs(UnmanagedType.LPUTF8Str)] string password,
                                                          IntPtr cancellable, out IntPtr error);

    [DllImport(SecretLib)]
    private static extern int secret_password_clearv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [DllImport(SecretLib)]
    private static extern void secret_password_free(IntPtr password);

    [DllImport(GLib)]
    private static extern IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr keyEqualFunc);

    [DllImport(GLib)]
    private static extern void g_hash_table_insert(IntPtr table, IntPtr key, IntPtr value);

    [DllImport(GLib)]
    private static extern void g_hash_table_unref(IntPtr table);

    [DllImport(GLib)]
    private static extern void g_error_free(IntPtr error);
}

/// <summary>
/// Linux without a keyring service (some lightweight desktops): keys.json next to settings.json, readable and writable
/// by this user only - the same protection as ~/.ssh keys. Not encrypted: without a keyring there's nowhere safe to keep
/// an encryption key.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxFileStore : ISecretStore
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private readonly string _path;
    private readonly Dictionary<string, string> _keys = new();

    public LinuxFileStore(string path)
    {
        _path = path;
        try
        {
            if (File.Exists(path))
                _keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
        }
        catch
        {
            // Unreadable - the user can re-enter their keys in Settings.
        }
    }

    public string Get(string name) => _keys.TryGetValue(name, out var value) ? value : "";

    public void Set(string name, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (!_keys.Remove(name)) return;
        }
        else
        {
            _keys[name] = value;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (File.Exists(_path)) File.SetUnixFileMode(_path, OwnerOnly); // tighten an older copy before writing keys to it
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, UnixCreateMode = OwnerOnly };
        using var writer = new StreamWriter(_path, options);
        writer.Write(JsonConvert.SerializeObject(_keys, Formatting.Indented));
    }
}

/// <summary>
/// Windows (for trying the app out on a PC): keys encrypted with DPAPI for the current Windows user,
/// in keys.json next to settings.json - the same protection WinPartsFinder uses.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsDpapiStore : ISecretStore
{
    private readonly string _path;
    private readonly Dictionary<string, string> _encrypted = new();

    public WindowsDpapiStore(string path)
    {
        _path = path;
        try
        {
            if (File.Exists(path))
                _encrypted = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
        }
        catch
        {
            // Unreadable - the user can re-enter their keys in Settings.
        }
    }

    public string Get(string name)
    {
        if (!_encrypted.TryGetValue(name, out var enc) || enc.Length == 0) return "";
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(enc), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return ""; // e.g. file copied from another Windows user
        }
    }

    public void Set(string name, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (!_encrypted.Remove(name)) return;
        }
        else
        {
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            _encrypted[name] = Convert.ToBase64String(bytes);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonConvert.SerializeObject(_encrypted, Formatting.Indented));
    }
}

/// <summary>Other systems: keys work while the app is open but aren't saved.</summary>
internal sealed class NoSecretStore : ISecretStore
{
    public string Get(string name) => "";

    public void Set(string name, string value)
    {
    }
}
