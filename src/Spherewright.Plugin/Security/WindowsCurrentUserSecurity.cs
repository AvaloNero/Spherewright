using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Spherewright.Plugin.Security;

internal static class WindowsCurrentUserSecurity
{
    private const uint TokenQuery = 0x0008;
    private const int TokenUser = 1;
    private const int ErrorInsufficientBuffer = 122;
    private const int ErrorAlreadyExists = 183;
    private const uint SecurityDescriptorRevision = 1;
    private const uint DaclSecurityInformation = 0x00000004;
    private const uint ProtectedDaclSecurityInformation = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint CreateNew = 1;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint PipeAccessDuplex = 0x00000003;
    private const uint FileFlagOverlapped = 0x40000000;
    private const uint PipeRejectRemoteClients = 0x00000008;

    private static readonly Lazy<string> CurrentUserSid = new Lazy<string>(ReadCurrentUserSid);

    public static void EnsureSecureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A secure directory path is required.", nameof(path));
        }

        using (var descriptor = NativeSecurityDescriptor.Create(BuildDirectorySddl(CurrentUserSid.Value)))
        {
            var missingDirectories = new Stack<string>();
            var current = path;
            while (!Directory.Exists(current))
            {
                missingDirectories.Push(current);
                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    throw new InvalidOperationException($"No existing parent was found for secure directory '{path}'.");
                }

                current = parent.FullName;
            }

            while (missingDirectories.Count > 0)
            {
                var directory = missingDirectories.Pop();
                var attributes = descriptor.CreateAttributes();
                if (!CreateDirectory(directory, ref attributes))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != ErrorAlreadyExists)
                    {
                        throw new Win32Exception(error, $"Could not create secure directory '{directory}'.");
                    }
                }

                ApplyProtectedDacl(directory, descriptor.Pointer);
            }

            ApplyProtectedDacl(path, descriptor.Pointer);
        }
    }

    public static void WriteSecureNewFile(string path, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A secure file path is required.", nameof(path));
        }

        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        using (var descriptor = NativeSecurityDescriptor.Create(BuildFileSddl(CurrentUserSid.Value)))
        {
            var attributes = descriptor.CreateAttributes();
            using (var handle = CreateFile(
                path,
                GenericWrite,
                0,
                ref attributes,
                CreateNew,
                FileAttributeNormal,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not create secure file '{path}'.");
                }

                var offset = 0;
                while (offset < content.Length)
                {
                    var remaining = content.Length - offset;
                    var chunk = new byte[remaining];
                    Buffer.BlockCopy(content, offset, chunk, 0, remaining);
                    if (!WriteFile(handle, chunk, (uint)chunk.Length, out var written, IntPtr.Zero))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not write secure file '{path}'.");
                    }

                    if (written == 0)
                    {
                        throw new IOException($"Writing secure file '{path}' made no progress.");
                    }

                    offset += checked((int)written);
                }

                if (!FlushFileBuffers(handle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not flush secure file '{path}'.");
                }
            }
        }
    }

    public static NamedPipeServerStream CreateSecurePipe(string pipeName, int inputBufferSize, int outputBufferSize)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("A Pipe name is required.", nameof(pipeName));
        }

        using (var descriptor = NativeSecurityDescriptor.Create(BuildPipeSddl(CurrentUserSid.Value)))
        {
            var attributes = descriptor.CreateAttributes();
            var handle = CreateNamedPipe(
                $@"\\.\pipe\{pipeName}",
                PipeAccessDuplex | FileFlagOverlapped,
                PipeRejectRemoteClients,
                1,
                checked((uint)outputBufferSize),
                checked((uint)inputBufferSize),
                0,
                ref attributes);
            if (handle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                handle.Dispose();
                throw new Win32Exception(error, $"Could not create secure Named Pipe '{pipeName}'.");
            }

            try
            {
                return new NamedPipeServerStream(PipeDirection.InOut, true, false, handle);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }
    }

    private static string ReadCurrentUserSid()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenQuery, out var tokenHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the current process token.");
        }

        try
        {
            GetTokenInformation(tokenHandle, TokenUser, IntPtr.Zero, 0, out var requiredLength);
            var firstError = Marshal.GetLastWin32Error();
            if (requiredLength <= 0 || firstError != ErrorInsufficientBuffer)
            {
                throw new Win32Exception(firstError, "Could not determine the current process-token user size.");
            }

            var tokenInformation = Marshal.AllocHGlobal(requiredLength);
            try
            {
                if (!GetTokenInformation(tokenHandle, TokenUser, tokenInformation, requiredLength, out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read the current process-token user.");
                }

                var sidPointer = Marshal.ReadIntPtr(tokenInformation);
                if (!ConvertSidToStringSid(sidPointer, out var stringSidPointer))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not convert the current user SID to text.");
                }

                try
                {
                    return Marshal.PtrToStringUni(stringSidPointer)
                        ?? throw new InvalidOperationException("Windows returned an empty current-user SID.");
                }
                finally
                {
                    LocalFree(stringSidPointer);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(tokenInformation);
            }
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }

    private static void ApplyProtectedDacl(string path, IntPtr securityDescriptor)
    {
        if (!SetFileSecurity(
            path,
            DaclSecurityInformation | ProtectedDaclSecurityInformation,
            securityDescriptor))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not protect the ACL for '{path}'.");
        }
    }

    private static string BuildDirectorySddl(string sid) => $"O:{sid}D:P(A;OICI;FA;;;{sid})";

    private static string BuildFileSddl(string sid) => $"O:{sid}D:P(A;;FA;;;{sid})";

    private static string BuildPipeSddl(string sid) => $"O:{sid}D:P(A;;GA;;;{sid})";

    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int Length;
        public IntPtr SecurityDescriptor;
        public int InheritHandle;
    }

    private sealed class NativeSecurityDescriptor : IDisposable
    {
        private NativeSecurityDescriptor(IntPtr pointer)
        {
            Pointer = pointer;
        }

        public IntPtr Pointer { get; private set; }

        public static NativeSecurityDescriptor Create(string sddl)
        {
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
                sddl,
                SecurityDescriptorRevision,
                out var pointer,
                out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create a current-user security descriptor.");
            }

            return new NativeSecurityDescriptor(pointer);
        }

        public SecurityAttributes CreateAttributes()
        {
            return new SecurityAttributes
            {
                Length = Marshal.SizeOf(typeof(SecurityAttributes)),
                SecurityDescriptor = Pointer,
                InheritHandle = 0,
            };
        }

        public void Dispose()
        {
            var pointer = Pointer;
            Pointer = IntPtr.Zero;
            if (pointer != IntPtr.Zero)
            {
                LocalFree(pointer);
            }
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        IntPtr tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertSidToStringSid(IntPtr sid, out IntPtr stringSid);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string stringSecurityDescriptor,
        uint stringSdRevision,
        out IntPtr securityDescriptor,
        out uint securityDescriptorSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileSecurity(
        string fileName,
        uint securityInformation,
        IntPtr securityDescriptor);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateDirectory(string pathName, ref SecurityAttributes securityAttributes);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        ref SecurityAttributes securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteFile(
        SafeFileHandle file,
        byte[] buffer,
        uint numberOfBytesToWrite,
        out uint numberOfBytesWritten,
        IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers(SafeFileHandle file);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePipeHandle CreateNamedPipe(
        string name,
        uint openMode,
        uint pipeMode,
        uint maxInstances,
        uint outputBufferSize,
        uint inputBufferSize,
        uint defaultTimeout,
        ref SecurityAttributes securityAttributes);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
