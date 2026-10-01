using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gum.Avalonia.Services.MacOS;

/// <summary>
/// Moves a file to the macOS trash with <c>NSFileManager trashItemAtURL:resultingItemURL:error:</c>
/// through the Objective-C runtime. Unlike asking Finder over Apple Events, this needs no Automation
/// consent, which the packaged app never declares (#5556). This is the head's sanctioned per-OS
/// P/Invoke file for Foundation (see BannedSymbols.CrossPlatform.txt); callers must check
/// <see cref="OperatingSystem.IsMacOS"/>.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacFileTrash
{
    private const string ObjCPath = "/usr/lib/libobjc.A.dylib";
    private const string FoundationPath = "/System/Library/Frameworks/Foundation.framework/Foundation";

    // The NSFileManager/NSURL/NSString classes live in Foundation, which the process may not have
    // loaded yet; objc_getClass only finds classes from loaded images.
    static MacFileTrash()
    {
        NativeLibrary.Load(FoundationPath);
    }

#pragma warning disable RS0030 // Sanctioned per-OS P/Invoke file for the Avalonia head.
    [DllImport(ObjCPath)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCPath)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCPath)]
    private static extern IntPtr objc_autoreleasePoolPush();

    [DllImport(ObjCPath)]
    private static extern void objc_autoreleasePoolPop(IntPtr pool);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr argument);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string argument);

    [DllImport(ObjCPath, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool SendTrashMessage(IntPtr receiver, IntPtr selector, IntPtr url,
        out IntPtr resultingUrl, out IntPtr error);
#pragma warning restore RS0030

    /// <summary>
    /// Moves the file or folder at <paramref name="fullPath"/> to the trash and returns where it
    /// landed (it may be renamed to avoid a name already in the trash).
    /// </summary>
    /// <exception cref="IOException">The file could not be trashed; the message carries the system's reason.</exception>
    public static string MoveToTrash(string fullPath)
    {
        IntPtr pool = objc_autoreleasePoolPush();
        try
        {
            IntPtr path = SendMessage(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), fullPath);
            IntPtr url = SendMessage(objc_getClass("NSURL"), sel_registerName("fileURLWithPath:"), path);
            IntPtr fileManager = SendMessage(objc_getClass("NSFileManager"), sel_registerName("defaultManager"));

            bool trashed = SendTrashMessage(fileManager, sel_registerName("trashItemAtURL:resultingItemURL:error:"),
                url, out IntPtr resultingUrl, out IntPtr error);
            if (!trashed)
            {
                string reason = ToManagedString(SendMessage(error, sel_registerName("localizedDescription")));
                throw new IOException($"Could not move {fullPath} to the trash: {reason}");
            }
            return ToManagedString(SendMessage(resultingUrl, sel_registerName("path")));
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    // Messaging nil returns nil in Objective-C, so a missing object reads back as an empty string.
    private static string ToManagedString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero)
        {
            return "";
        }
        return Marshal.PtrToStringUTF8(SendMessage(nsString, sel_registerName("UTF8String"))) ?? "";
    }
}
