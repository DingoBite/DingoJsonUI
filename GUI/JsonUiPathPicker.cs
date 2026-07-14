#if NEWTONSOFT_EXISTS
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DingoJsonUI.GUI
{
    public static class JsonUiPathPicker
    {
        public static string OpenFile(string title, string directory, string extension)
        {
#if UNITY_EDITOR
            var path = EditorUtility.OpenFilePanel(
                string.IsNullOrWhiteSpace(title) ? "Select file" : title,
                NormalizeDirectory(directory),
                ToUnityEditorExtension(extension));
            return string.IsNullOrWhiteSpace(path) ? null : path;
#elif UNITY_STANDALONE_WIN
            return WindowsFileDialog.OpenFile(title, directory, extension);
#else
            Debug.LogWarning("DingoJsonUI file picker is implemented for Unity Editor and Windows standalone builds.");
            return null;
#endif
        }

        public static string OpenFolder(string title, string directory)
        {
#if UNITY_EDITOR
            var path = EditorUtility.OpenFolderPanel(
                string.IsNullOrWhiteSpace(title) ? "Select folder" : title,
                NormalizeDirectory(directory),
                string.Empty);
            return string.IsNullOrWhiteSpace(path) ? null : path;
#elif UNITY_STANDALONE_WIN
            return WindowsFileDialog.OpenFolder(title, directory);
#else
            Debug.LogWarning("DingoJsonUI folder picker is implemented for Unity Editor and Windows standalone builds.");
            return null;
#endif
        }

        public static string SaveFile(string title, string directory, string defaultName, string extension)
        {
#if UNITY_EDITOR
            var path = EditorUtility.SaveFilePanel(
                string.IsNullOrWhiteSpace(title) ? "Save file" : title,
                NormalizeDirectory(directory),
                string.IsNullOrWhiteSpace(defaultName) ? "untitled" : defaultName,
                ToUnityEditorExtension(extension));
            return string.IsNullOrWhiteSpace(path) ? null : path;
#elif UNITY_STANDALONE_WIN
            return WindowsFileDialog.SaveFile(title, directory, defaultName, extension);
#else
            Debug.LogWarning("DingoJsonUI save file picker is implemented for Unity Editor and Windows standalone builds.");
            return null;
#endif
        }

        public static bool Confirm(string title, string message, string confirmLabel = "Continue")
        {
#if UNITY_EDITOR
            return EditorUtility.DisplayDialog(
                string.IsNullOrWhiteSpace(title) ? "Confirm" : title,
                message ?? string.Empty,
                string.IsNullOrWhiteSpace(confirmLabel) ? "Continue" : confirmLabel,
                "Cancel");
#elif UNITY_STANDALONE_WIN
            return WindowsFileDialog.Confirm(title, message);
#else
            Debug.LogWarning("DingoJsonUI confirmation dialog is implemented for Unity Editor and Windows standalone builds.");
            return false;
#endif
        }

        private static string NormalizeDirectory(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                return directory;

            return Application.dataPath;
        }

        private static string ToUnityEditorExtension(string extension)
        {
            var extensions = SplitExtensions(extension);
            return extensions.Length == 1 ? extensions[0] : string.Empty;
        }

        private static string[] SplitExtensions(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return Array.Empty<string>();

            var parts = extension.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim().TrimStart('.').ToLowerInvariant();

            return parts;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static class WindowsFileDialog
        {
            private const int HResultCancelled = unchecked((int)0x800704C7);
            private const uint MessageBoxOkCancel = 0x00000001;
            private const uint MessageBoxIconWarning = 0x00000030;
            private const uint MessageBoxDefaultButton2 = 0x00000100;
            private const int MessageBoxOk = 1;

            public static bool Confirm(string title, string message)
            {
                return MessageBox(
                           IntPtr.Zero,
                           message ?? string.Empty,
                           string.IsNullOrWhiteSpace(title) ? "Confirm" : title,
                           MessageBoxOkCancel | MessageBoxIconWarning | MessageBoxDefaultButton2)
                       == MessageBoxOk;
            }

            public static string OpenFile(string title, string directory, string extension)
            {
                return Open(title, directory, extension, pickFolders: false);
            }

            public static string OpenFolder(string title, string directory)
            {
                return Open(title, directory, null, pickFolders: true);
            }

            public static string SaveFile(string title, string directory, string defaultName, string extension)
            {
                IFileSaveDialog dialog = null;
                IShellItem resultItem = null;
                IShellItem defaultFolder = null;

                try
                {
                    dialog = (IFileSaveDialog)(object)new FileSaveDialog();
                    dialog.GetOptions(out var options);
                    options |= FileOpenOptions.ForceFileSystem | FileOpenOptions.PathMustExist | FileOpenOptions.NoChangeDirectory | FileOpenOptions.OverwritePrompt;
                    dialog.SetOptions(options);

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        dialog.SetTitle(title);
                    }

                    ConfigureFileTypes(dialog, extension);

                    if (!string.IsNullOrWhiteSpace(defaultName))
                    {
                        dialog.SetFileName(defaultName);
                    }

                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        var shellItemGuid = typeof(IShellItem).GUID;
                        SHCreateItemFromParsingName(directory, IntPtr.Zero, ref shellItemGuid, out defaultFolder);
                        if (defaultFolder != null)
                        {
                            dialog.SetDefaultFolder(defaultFolder);
                        }
                    }

                    var hr = dialog.Show(IntPtr.Zero);
                    if (hr == HResultCancelled)
                    {
                        return null;
                    }
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    dialog.GetResult(out resultItem);
                    return GetShellItemPath(resultItem);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"DingoJsonUI save picker failed: {e.Message}");
                    return null;
                }
                finally
                {
                    ReleaseComObject(resultItem);
                    ReleaseComObject(defaultFolder);
                    ReleaseComObject(dialog);
                }
            }

            private static string Open(string title, string directory, string extension, bool pickFolders)
            {
                IFileOpenDialog dialog = null;
                IShellItem resultItem = null;
                IShellItem defaultFolder = null;

                try
                {
                    dialog = (IFileOpenDialog)(object)new FileOpenDialog();
                    dialog.GetOptions(out var options);
                    options |= FileOpenOptions.ForceFileSystem | FileOpenOptions.PathMustExist | FileOpenOptions.NoChangeDirectory;
                    options |= pickFolders ? FileOpenOptions.PickFolders : FileOpenOptions.FileMustExist;
                    dialog.SetOptions(options);

                    if (!string.IsNullOrWhiteSpace(title))
                        dialog.SetTitle(title);

                    if (!pickFolders)
                        ConfigureFileTypes(dialog, extension);

                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        var shellItemGuid = typeof(IShellItem).GUID;
                        SHCreateItemFromParsingName(directory, IntPtr.Zero, ref shellItemGuid, out defaultFolder);
                        if (defaultFolder != null)
                            dialog.SetDefaultFolder(defaultFolder);
                    }

                    var hr = dialog.Show(IntPtr.Zero);
                    if (hr == HResultCancelled)
                        return null;
                    if (hr < 0)
                        Marshal.ThrowExceptionForHR(hr);

                    dialog.GetResult(out resultItem);
                    return GetShellItemPath(resultItem);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"DingoJsonUI path picker failed: {e.Message}");
                    return null;
                }
                finally
                {
                    ReleaseComObject(resultItem);
                    ReleaseComObject(defaultFolder);
                    ReleaseComObject(dialog);
                }
            }

            private static void ConfigureFileTypes(IFileOpenDialog dialog, string extension)
            {
                var extensions = SplitExtensions(extension);
                if (extensions.Length == 0)
                    return;

                var specs = new COMDLG_FILTERSPEC[2];
                var filter = string.Join(";", Array.ConvertAll(extensions, ext => "*." + ext));
                specs[0] = new COMDLG_FILTERSPEC
                {
                    pszName = string.Join(", ", Array.ConvertAll(extensions, ext => "." + ext.ToUpperInvariant())),
                    pszSpec = filter,
                };
                specs[1] = new COMDLG_FILTERSPEC
                {
                    pszName = "All files",
                    pszSpec = "*.*",
                };

                dialog.SetFileTypes((uint)specs.Length, specs);
                dialog.SetFileTypeIndex(1);
                dialog.SetDefaultExtension(extensions[0]);
            }

            private static void ConfigureFileTypes(IFileSaveDialog dialog, string extension)
            {
                var extensions = SplitExtensions(extension);
                if (extensions.Length == 0)
                    return;

                var specs = new COMDLG_FILTERSPEC[2];
                var filter = string.Join(";", Array.ConvertAll(extensions, ext => "*." + ext));
                specs[0] = new COMDLG_FILTERSPEC
                {
                    pszName = string.Join(", ", Array.ConvertAll(extensions, ext => "." + ext.ToUpperInvariant())),
                    pszSpec = filter,
                };
                specs[1] = new COMDLG_FILTERSPEC
                {
                    pszName = "All files",
                    pszSpec = "*.*",
                };

                dialog.SetFileTypes((uint)specs.Length, specs);
                dialog.SetFileTypeIndex(1);
                dialog.SetDefaultExtension(extensions[0]);
            }

            private static string GetShellItemPath(IShellItem item)
            {
                if (item == null)
                    return null;

                item.GetDisplayName(ShellItemDisplayName.FileSystemPath, out var pointer);
                if (pointer == IntPtr.Zero)
                    return null;

                try
                {
                    return Marshal.PtrToStringUni(pointer);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pointer);
                }
            }

            private static void ReleaseComObject(object instance)
            {
                if (instance != null && Marshal.IsComObject(instance))
                    Marshal.FinalReleaseComObject(instance);
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            private static extern void SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string path,
                IntPtr bindContext,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
            private static extern int MessageBox(IntPtr owner, string text, string caption, uint type);

            [ComImport]
            [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
            private sealed class FileOpenDialog
            {
            }

            [ComImport]
            [Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
            private sealed class FileSaveDialog
            {
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
            private interface IFileOpenDialog
            {
                [PreserveSig]
                int Show(IntPtr owner);
                void SetFileTypes(uint fileTypesCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] filterSpec);
                void SetFileTypeIndex(uint fileTypeIndex);
                void GetFileTypeIndex(out uint fileTypeIndex);
                void Advise(IntPtr events, out uint cookie);
                void Unadvise(uint cookie);
                void SetOptions(FileOpenOptions options);
                void GetOptions(out FileOpenOptions options);
                void SetDefaultFolder(IShellItem shellItem);
                void SetFolder(IShellItem shellItem);
                void GetFolder(out IShellItem shellItem);
                void GetCurrentSelection(out IShellItem shellItem);
                void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string fileName);
                void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
                void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
                void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
                void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
                void GetResult(out IShellItem shellItem);
                void AddPlace(IShellItem shellItem, int fileDialogAddPlace);
                void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string defaultExtension);
                void Close(int hresult);
                void SetClientGuid(ref Guid guid);
                void ClearClientData();
                void SetFilter(IntPtr filter);
                void GetResults(IntPtr items);
                void GetSelectedItems(IntPtr items);
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("84bccd23-5fde-4cdb-aea4-af64b83d78ab")]
            private interface IFileSaveDialog
            {
                [PreserveSig]
                int Show(IntPtr owner);
                void SetFileTypes(uint fileTypesCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] filterSpec);
                void SetFileTypeIndex(uint fileTypeIndex);
                void GetFileTypeIndex(out uint fileTypeIndex);
                void Advise(IntPtr events, out uint cookie);
                void Unadvise(uint cookie);
                void SetOptions(FileOpenOptions options);
                void GetOptions(out FileOpenOptions options);
                void SetDefaultFolder(IShellItem shellItem);
                void SetFolder(IShellItem shellItem);
                void GetFolder(out IShellItem shellItem);
                void GetCurrentSelection(out IShellItem shellItem);
                void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string fileName);
                void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
                void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
                void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
                void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
                void GetResult(out IShellItem shellItem);
                void AddPlace(IShellItem shellItem, int fileDialogAddPlace);
                void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string defaultExtension);
                void Close(int hresult);
                void SetClientGuid(ref Guid guid);
                void ClearClientData();
                void SetFilter(IntPtr filter);
                void SetSaveAsItem(IShellItem shellItem);
                void SetProperties(IntPtr propertyStore);
                void SetCollectedProperties(IntPtr propertyDescriptionList, bool appendDefault);
                void GetProperties(out IntPtr propertyStore);
                void ApplyProperties(IShellItem shellItem, IntPtr propertyStore, IntPtr hwnd, IntPtr sink);
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
            private interface IShellItem
            {
                void BindToHandler(IntPtr bindContext, ref Guid bhid, ref Guid riid, out IntPtr ppv);
                void GetParent(out IShellItem parent);
                void GetDisplayName(ShellItemDisplayName sigdnName, out IntPtr displayName);
                void GetAttributes(uint sfgaoMask, out uint sfgaoAttribs);
                void Compare(IShellItem shellItem, uint hint, out int order);
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct COMDLG_FILTERSPEC
            {
                [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
                [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
            }

            [Flags]
            private enum FileOpenOptions : uint
            {
                FileMustExist = 0x00001000,
                PathMustExist = 0x00000800,
                NoChangeDirectory = 0x00000008,
                PickFolders = 0x00000020,
                ForceFileSystem = 0x00000040,
                OverwritePrompt = 0x00000002,
            }

            private enum ShellItemDisplayName : uint
            {
                FileSystemPath = 0x80058000,
            }
        }
#endif
    }
}
#endif
