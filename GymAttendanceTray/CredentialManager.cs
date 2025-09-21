using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace GymAttendanceTray;

public static class CredentialManager
{
    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr CredentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] UInt32 flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, CRED_TYPE type, int reservedFlag);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    private static extern bool CredFree([In] IntPtr cred);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public UInt32 Flags;
        public CRED_TYPE Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public FILETIME LastWritten;
        public UInt32 CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public UInt32 AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public UInt32 dwLowDateTime;
        public UInt32 dwHighDateTime;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7,
        MAXIMUM_EX = 1007
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }

    public class UserCredential
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Target { get; set; }
    }

    public static void WriteCredential(string target, string userName, string password, string comment)
    {
        var byteArray = Encoding.Unicode.GetBytes(password);
        var credential = new CREDENTIAL
        {
            AttributeCount = 0,
            Attributes = IntPtr.Zero,
            Comment = Marshal.StringToCoTaskMemUni(comment),
            TargetAlias = IntPtr.Zero,
            Type = CRED_TYPE.GENERIC,
            Persist = CRED_PERSIST.LOCAL_MACHINE,
            CredentialBlobSize = (UInt32)byteArray.Length,
            TargetName = Marshal.StringToCoTaskMemUni(target),
            CredentialBlob = Marshal.StringToCoTaskMemUni(password),
            UserName = Marshal.StringToCoTaskMemUni(userName)
        };

        var written = CredWrite(ref credential, 0);
        var lastError = Marshal.GetLastWin32Error();

        if (credential.TargetName != IntPtr.Zero)
            Marshal.FreeCoTaskMem(credential.TargetName);
        if (credential.UserName != IntPtr.Zero)
            Marshal.FreeCoTaskMem(credential.UserName);
        if (credential.CredentialBlob != IntPtr.Zero)
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
        if (credential.Comment != IntPtr.Zero)
            Marshal.FreeCoTaskMem(credential.Comment);

        if (!written)
        {
            throw new Exception($"Failed to write credential. Error: {lastError}");
        }
    }

    public static UserCredential? ReadCredential(string target)
    {
        var read = CredRead(target, CRED_TYPE.GENERIC, 0, out IntPtr nCredPtr);
        var lastError = Marshal.GetLastWin32Error();

        if (!read)
        {
            return null;
        }

        using (var critCred = new CriticalCredentialHandle(nCredPtr))
        {
            var cred = critCred.GetCredential();
            var username = Marshal.PtrToStringUni(cred.UserName);
            var password = Marshal.PtrToStringUni(cred.CredentialBlob, (int)cred.CredentialBlobSize / 2);

            return new UserCredential
            {
                UserName = username,
                Password = password,
                Target = target
            };
        }
    }

    public static void DeleteCredential(string target)
    {
        var deleted = CredDelete(target, CRED_TYPE.GENERIC, 0);
        var lastError = Marshal.GetLastWin32Error();

        if (!deleted)
        {
            throw new Exception($"Failed to delete credential. Error: {lastError}");
        }
    }

    private sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        public CriticalCredentialHandle(IntPtr preexistingHandle)
        {
            SetHandle(preexistingHandle);
        }

        public CREDENTIAL GetCredential()
        {
            if (!IsInvalid)
            {
                return Marshal.PtrToStructure<CREDENTIAL>(handle);
            }

            throw new InvalidOperationException("Invalid credential handle");
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                CredFree(handle);
                SetHandleAsInvalid();
                return true;
            }

            return false;
        }
    }
}