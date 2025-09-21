
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.IO;
using ScriptableFramework;
using DotnetStoryScript;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("[csharp] Program.Main");
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct HostApi
{
    public IntPtr OutputLog;
    public IntPtr ShowProgress;
    public IntPtr RunCommand;
    public IntPtr RunCommandTimeout;
    public IntPtr GetResultCode;
    public IntPtr GetErrorCount;
    public IntPtr GetOutputCount;
    public IntPtr GetError;
    public IntPtr GetOutput;
    public IntPtr FindInPath;
    public IntPtr GetAdbExe;
    public IntPtr GetApkToolJar;
    public IntPtr GetJadxExe;
    public IntPtr GetJavaExe;
    public IntPtr GetUberApkSignerJar;
    public IntPtr GetZipAlignExe;
    public IntPtr GetJavaHeap;
}

// delegate for native api
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate void HostOutputLogDelegation(string msg);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate void HostShowProgressDelegation(int percent, string msg);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostRunCommandDelegation(string cmd, string args, IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostRunCommandTimeoutDelegation(string cmd, string args, int timeout, IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int HostGetResultCodeDelegation(IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int HostGetErrorCountDelegation(IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int HostGetOutputCountDelegation(IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetErrorDelegation(int index, StringBuilder path, ref int path_size, IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetOutputDelegation(int index, StringBuilder path, ref int path_size, IntPtr result);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostFindInPathDelegation(string filename, StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetAdbExeDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetApkToolJarDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetJadxExeDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetJavaExeDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetUberApkSignerJarDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public delegate bool HostGetZipAlignExeDelegation(StringBuilder path, ref int path_size);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int HostGetJavaHeapDelegation();

namespace DotNetLib
{
    sealed class OutpuLogExp : DotnetStoryScript.DslExpression.SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            string fmt = string.Empty;
            var al = new System.Collections.ArrayList();
            for (int ix = 0; ix < operands.Count; ix++) {
                BoxedValue v = operands[ix];
                if (ix == 0) {
                    fmt = v.AsString;
                }
                else {
                    al.Add(v.GetObject());
                }
            }
            string str = string.Format(fmt, al.ToArray());
            Lib.LogNoLock(str);
            return str;
        }
    }
    sealed class WriteFileExp : DotnetStoryScript.DslExpression.SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                string path = operands[0].GetString();
                string txt = operands[1].GetString();
                path = Path.Combine(Lib.BasePath, path);
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, txt);
            }
            return r;
        }
    }
    public class NativeApi
    {
        public NativeApi(IntPtr apis)
        {
            HostApi hostApi = Marshal.PtrToStructure<HostApi>(apis);
            m_OutputLogApi = Marshal.GetDelegateForFunctionPointer<HostOutputLogDelegation>(hostApi.OutputLog);
            m_ShowProgressApi = Marshal.GetDelegateForFunctionPointer<HostShowProgressDelegation>(hostApi.ShowProgress);
            m_RunCommandApi = Marshal.GetDelegateForFunctionPointer<HostRunCommandDelegation>(hostApi.RunCommand);
            m_RunCommandTimeoutApi = Marshal.GetDelegateForFunctionPointer<HostRunCommandTimeoutDelegation>(hostApi.RunCommandTimeout);
            m_GetResultCodeApi = Marshal.GetDelegateForFunctionPointer<HostGetResultCodeDelegation>(hostApi.GetResultCode);
            m_GetErrorCountApi = Marshal.GetDelegateForFunctionPointer<HostGetErrorCountDelegation>(hostApi.GetErrorCount);
            m_GetOutputCountApi = Marshal.GetDelegateForFunctionPointer<HostGetOutputCountDelegation>(hostApi.GetOutputCount);
            m_GetErrorApi = Marshal.GetDelegateForFunctionPointer<HostGetErrorDelegation>(hostApi.GetError);
            m_GetOutputApi = Marshal.GetDelegateForFunctionPointer<HostGetOutputDelegation>(hostApi.GetOutput);
            m_FindInPathApi = Marshal.GetDelegateForFunctionPointer<HostFindInPathDelegation>(hostApi.FindInPath);
            m_GetAdbExeApi = Marshal.GetDelegateForFunctionPointer<HostGetAdbExeDelegation>(hostApi.GetAdbExe);
            m_GetApkToolJarApi = Marshal.GetDelegateForFunctionPointer<HostGetApkToolJarDelegation>(hostApi.GetApkToolJar);
            m_GetJadxExeApi = Marshal.GetDelegateForFunctionPointer<HostGetJadxExeDelegation>(hostApi.GetJadxExe);
            m_GetJavaExeApi = Marshal.GetDelegateForFunctionPointer<HostGetJavaExeDelegation>(hostApi.GetJavaExe);
            m_GetUberApkSignerJarApi = Marshal.GetDelegateForFunctionPointer<HostGetUberApkSignerJarDelegation>(hostApi.GetUberApkSignerJar);
            m_GetZipAlignExeApi = Marshal.GetDelegateForFunctionPointer<HostGetZipAlignExeDelegation>(hostApi.GetZipAlignExe);
            m_GetJavaHeapApi = Marshal.GetDelegateForFunctionPointer<HostGetJavaHeapDelegation>(hostApi.GetJavaHeap);
        }

        public IntPtr Worker { get => m_Worker; set => m_Worker = value; }
        public IntPtr Result { get => m_Result; set => m_Result = value; }

        public void OutputLog(string msg)
        {
            if (m_OutputLogApi == null) {
                return;
            }
            m_OutputLogApi.Invoke(msg);
        }
        public void ShowProgress(int percent, string msg)
        {
            if (m_ShowProgressApi == null) {
                return;
            }
            m_ShowProgressApi.Invoke(percent, msg);
        }
        public bool RunCommand(string cmd, string args)
        {
            if (m_RunCommandApi == null) {
                return false;
            }
            return m_RunCommandApi.Invoke(cmd, args, m_Result);
        }
        public bool RunCommandTimeout(string cmd, string args, int timeout)
        {
            if (m_RunCommandTimeoutApi == null) {
                return false;
            }
            return m_RunCommandTimeoutApi.Invoke(cmd, args, timeout, m_Result);
        }
        public int GetResultCode()
        {
            if (m_GetResultCodeApi == null) {
                return 0;
            }
            return m_GetResultCodeApi.Invoke(m_Result);
        }
        public int GetErrorCount()
        {
            if (m_GetErrorCountApi == null) {
                return 0;
            }
            return m_GetErrorCountApi.Invoke(m_Result);
        }
        public int GetOutputCount()
        {
            if (m_GetOutputCountApi == null) {
                return 0;
            }
            return m_GetOutputCountApi.Invoke(m_Result);
        }
        public string GetError(int index)
        {    
            if (m_GetErrorApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_info_length + 1);
            int len = sb.Capacity;
            if (m_GetErrorApi.Invoke(index, sb, ref len, m_Result)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetOutput(int index)
        {
            if (m_GetOutputApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_info_length + 1);
            int len = sb.Capacity;
            if (m_GetOutputApi.Invoke(index, sb, ref len, m_Result)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string FindInPath(string fileName)
        {
            if (m_FindInPathApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_FindInPathApi.Invoke(fileName, sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetAdbExe()
        {
            if (m_GetAdbExeApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetAdbExeApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetApkToolJar()
        {
            if (m_GetApkToolJarApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetApkToolJarApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetJadxExe()
        {
            if (m_GetJadxExeApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetJadxExeApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetJavaExe()
        {
            if (m_GetJavaExeApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetJavaExeApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetUberApkSignerJar()
        {
            if (m_GetUberApkSignerJarApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetUberApkSignerJarApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public string GetZipAlignExe()
        {
            if (m_GetZipAlignExeApi == null) {
                return string.Empty;
            }
            var sb = new StringBuilder(c_max_path_length + 1);
            int len = sb.Capacity;
            if (m_GetZipAlignExeApi.Invoke(sb, ref len)) {
                return sb.ToString();
            }
            return string.Empty;
        }
        public int GetJavaHeap()
        {
            if (m_GetJavaHeapApi == null) {
                return 256;
            }
            return m_GetJavaHeapApi.Invoke();
        }

        private HostOutputLogDelegation? m_OutputLogApi;
        private HostShowProgressDelegation? m_ShowProgressApi;
        private HostRunCommandDelegation? m_RunCommandApi;
        private HostRunCommandTimeoutDelegation? m_RunCommandTimeoutApi;
        private HostGetResultCodeDelegation? m_GetResultCodeApi;
        private HostGetErrorCountDelegation? m_GetErrorCountApi;
        private HostGetOutputCountDelegation? m_GetOutputCountApi;
        private HostGetErrorDelegation? m_GetErrorApi;
        private HostGetOutputDelegation? m_GetOutputApi;
        private HostFindInPathDelegation? m_FindInPathApi;
        private HostGetAdbExeDelegation? m_GetAdbExeApi;
        private HostGetApkToolJarDelegation? m_GetApkToolJarApi;
        private HostGetJadxExeDelegation? m_GetJadxExeApi;
        private HostGetJavaExeDelegation? m_GetJavaExeApi;
        private HostGetUberApkSignerJarDelegation? m_GetUberApkSignerJarApi;
        private HostGetZipAlignExeDelegation? m_GetZipAlignExeApi;
        private HostGetJavaHeapDelegation? m_GetJavaHeapApi;

        private IntPtr m_Worker = IntPtr.Zero;
        private IntPtr m_Result = IntPtr.Zero;

        private const int c_max_path_length = 1024;
        private const int c_max_info_length = 4096;
    }
    public static class Lib
    {
        [UnmanagedCallersOnly]
        public static int RegisterApi(IntPtr apis)
        {
            s_NativeApi = new NativeApi(apis);

            return 0;
        }

        public delegate void InitDelegation(string path, IntPtr result);
        public delegate int DecompileApkDelegation(string apk, string folder, bool java, bool res, bool smali, IntPtr worker, IntPtr result);
        public delegate int RecompileApkDelegation(string folder, IntPtr worker, IntPtr result);
        public delegate int SignApkDelegation(string apk, string key, string keypwd, string alias, string aliaspwd, bool zipalign, IntPtr worker, IntPtr result);
        public delegate int InstallApkDelegation(string apk, IntPtr worker, IntPtr result);
        public delegate int GetZipAlignArgsDelegation(string folder, StringBuilder args, ref int argsLength);

        public static void Init(string path, IntPtr result)
        {
            s_MainThreadId = Thread.CurrentThread.ManagedThreadId;

            LogNoLock("[csharp] Init BasePath: " + path);
            s_BasePath = path;

            try {
                LogNoLock(string.Format("[csharp] Call dsl init"));

                if (null != s_NativeApi) {
                    s_NativeApi.Worker = IntPtr.Zero;
                    s_NativeApi.Result = result;

                    TryLoadDSL();
                    Calculator.SetGlobalVariable("nativeapi", BoxedValue.FromObject(s_NativeApi));
                    Calculator.SetGlobalVariable("basepath", BoxedValue.FromString(s_BasePath));
                    BoxedValue r = Calculator.Calc("init");
                }
            }
            catch (Exception e) {
                LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
            }
        }
        // -1 -- failed 0 -- nothing was done 1 -- finished
        public static int DecompileApk(string apk, string folder, bool java, bool res, bool smali, IntPtr worker, IntPtr result)
        {
            lock (s_Lock) {
                try {
                    LogNoLock(string.Format("[csharp] Call dsl decompile, apk:{0} folder:{1} java:{2} res:{3} smali:{4}", apk, folder, java, res, smali));

                    if (null != s_NativeApi) {
                        s_NativeApi.Worker = worker;
                        s_NativeApi.Result = result;

                        TryLoadDSL();
                        Calculator.SetGlobalVariable("nativeapi", BoxedValue.FromObject(s_NativeApi));
                        Calculator.SetGlobalVariable("basepath", BoxedValue.FromString(s_BasePath));
                        Calculator.SetGlobalVariable("apk", BoxedValue.FromString(apk));
                        Calculator.SetGlobalVariable("folder", BoxedValue.FromString(folder));
                        var args = Calculator.NewCalculatorValueList();
                        args.Add(BoxedValue.FromBool(java));
                        args.Add(BoxedValue.FromBool(res));
                        args.Add(BoxedValue.FromBool(smali));
                        BoxedValue r = Calculator.Calc("decompileapk", args);
                        Calculator.RecycleCalculatorValueList(args);
                        if (r.IsInteger) {
                            return r.GetInt();
                        }
                    }
                }
                catch (Exception e) {
                    LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
                }
                return 0;
            }
        }
        public static int RecompileApk(string folder, IntPtr worker, IntPtr result)
        {
            lock (s_Lock) {
                try {
                    LogNoLock(string.Format("[csharp] Call dsl decompile, folder:{0}", folder));

                    if (null != s_NativeApi) {
                        s_NativeApi.Worker = worker;
                        s_NativeApi.Result = result;

                        if (!GetApkPathAfterBuild(folder, out var sourceApkPath, out var targetApkPath)) {
                            return -1;
                        }

                        TryLoadDSL();
                        Calculator.SetGlobalVariable("nativeapi", BoxedValue.FromObject(s_NativeApi));
                        Calculator.SetGlobalVariable("basepath", BoxedValue.FromString(s_BasePath));
                        Calculator.SetGlobalVariable("folder", BoxedValue.FromString(folder));
                        var args = Calculator.NewCalculatorValueList();
                        args.Add(BoxedValue.FromString(sourceApkPath));
                        args.Add(BoxedValue.FromString(targetApkPath));
                        BoxedValue r = Calculator.Calc("recompileapk", args);
                        Calculator.RecycleCalculatorValueList(args);
                        if (r.IsInteger) {
                            return r.GetInt();
                        }
                    }
                }
                catch (Exception e) {
                    LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
                }
                return 0;
            }
        }
        public static int SignApk(string apk, string key, string keypwd, string alias, string aliaspwd, bool zipalign, IntPtr worker, IntPtr result)
        {
            lock (s_Lock) {
                try {
                    LogNoLock(string.Format("[csharp] Call dsl sign, apk:{0} key:{1} keypwd:{2} alias:{3} aliaspwd:{4} zipalign:{5}", apk, key, keypwd, alias, aliaspwd, zipalign));

                    if (null != s_NativeApi) {
                        s_NativeApi.Worker = worker;
                        s_NativeApi.Result = result;

                        string target = GetTargetApkPath(apk);

                        TryLoadDSL();
                        Calculator.SetGlobalVariable("nativeapi", BoxedValue.FromObject(s_NativeApi));
                        Calculator.SetGlobalVariable("basepath", BoxedValue.FromString(s_BasePath));
                        Calculator.SetGlobalVariable("apk", BoxedValue.FromString(apk));
                        var args = Calculator.NewCalculatorValueList();
                        args.Add(BoxedValue.FromString(key));
                        args.Add(BoxedValue.FromString(keypwd));
                        args.Add(BoxedValue.FromString(alias));
                        args.Add(BoxedValue.FromString(aliaspwd));
                        args.Add(BoxedValue.FromBool(zipalign));
                        args.Add(BoxedValue.FromString(target));
                        BoxedValue r = Calculator.Calc("signapk", args);
                        Calculator.RecycleCalculatorValueList(args);
                        if (r.IsInteger) {
                            return r.GetInt();
                        }
                    }
                }
                catch (Exception e) {
                    LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
                }
                return 0;
            }
        }
        public static int InstallApk(string apk, IntPtr worker, IntPtr result)
        {
            lock (s_Lock) {
                try {
                    LogNoLock(string.Format("[csharp] Call dsl installapk, apk:{0}", apk));

                    if (null != s_NativeApi) {
                        s_NativeApi.Worker = worker;
                        s_NativeApi.Result = result;

                        TryLoadDSL();
                        Calculator.SetGlobalVariable("nativeapi", BoxedValue.FromObject(s_NativeApi));
                        Calculator.SetGlobalVariable("basepath", BoxedValue.FromString(s_BasePath));
                        Calculator.SetGlobalVariable("apk", BoxedValue.FromString(apk));
                        BoxedValue r = Calculator.Calc("installapk");
                        if (r.IsInteger) {
                            return r.GetInt();
                        }
                    }
                }
                catch (Exception e) {
                    LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
                }
                return 0;
            }
        }
        public static int GetZipAlignArgs(string folder, StringBuilder args, ref int argsLength)
        {
            lock (s_Lock) {
                try {
                    if (!GetApkPathAfterBuild(folder, out var sourceApkPath, out var targetApkPath)) {
                        return -1;
                    }

                    LogNoLock(string.Format("[csharp] Call dsl script, source:{0} target:{1}", sourceApkPath, targetApkPath));

                    TryLoadDSL();
                    Calculator.SetGlobalVariable("folder", BoxedValue.FromString(folder));
                    var sargs = Calculator.NewCalculatorValueList();
                    sargs.Add(BoxedValue.FromString(sourceApkPath));
                    sargs.Add(BoxedValue.FromString(targetApkPath));
                    BoxedValue r = Calculator.Calc("get_zipalign_args", sargs);
                    Calculator.RecycleCalculatorValueList(sargs);

                    args.Clear();
                    if (r.IsString) {
                        args.Append(r.ToString());
                        argsLength = args.Length;
                    }
                }
                catch (Exception e) {
                    LogNoLock("[csharp] Exception:" + e.Message + "\n" + e.StackTrace);
                }
            }
            return 1;
        }
        private static bool GetApkPathAfterBuild(string folder, out string source, out string target)
        {
            source = string.Empty;
            target = string.Empty;

            string path = Path.Combine(folder, "apktool.yml");
            if (!File.Exists(path)) {
                LogNoLock("[csharp] Can't find file: " + path);
                return false;
            }
            string apkFileName = string.Empty;
            using (var sr = File.OpenText(path)) {
                while (!sr.EndOfStream) {
                    string? line = sr.ReadLine();
                    if (null == line) {
                        break;
                    }
                    if (line.StartsWith(c_apk_file_name_key)) {
                        apkFileName = line.Substring(c_apk_file_name_key.Length).Trim();
                        break;
                    }
                }
            }

            string distDir = Path.Combine(folder, "dist");
            string sourceApkPath = Path.Combine(distDir, apkFileName);
            string targetApkPath = GetTargetApkPath(sourceApkPath);

            source = sourceApkPath;
            target = targetApkPath;
            return true;
        }
        private static string GetTargetApkPath(string sourceApkPath)
        {
            string? dir = Path.GetDirectoryName(sourceApkPath);
            Debug.Assert(dir != null);
            string apkFileName = Path.GetFileName(sourceApkPath);
            string apkFileNameWithoutExt = Path.GetFileNameWithoutExtension(apkFileName);
            string targetApkFileName;
            if (apkFileNameWithoutExt.EndsWith(c_unshell_sign_suffix)) {
                targetApkFileName = apkFileNameWithoutExt.Substring(0, apkFileNameWithoutExt.Length - c_unshell_sign_suffix.Length) + ".apk";
            }
            else {
                targetApkFileName = apkFileNameWithoutExt + "_t.apk";
            }
            return Path.Combine(dir, targetApkFileName);
        }

        public static string BasePath
        {
            get {
                return s_BasePath;
            }
        }
        public static void LogNoLock(string msg)
        {
            if (null != s_NativeApi) {
                bool isMainThread = Thread.CurrentThread.ManagedThreadId == s_MainThreadId;
                string txt = string.Format("thread:{0} {1}{2}: {3}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Name, isMainThread ? "(main)" : string.Empty, msg);
                //Console.WriteLine(txt);
                var lines = txt.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines) {
                    s_NativeApi.OutputLog(line);
                }
            }
        }
        private static void TryLoadDSL()
        {
            var fi = new FileInfo(Path.Combine(s_BasePath, "../Script.dsl"));
            if (fi.Exists) {
                if (fi.LastWriteTime != s_DslScriptTime) {
                    s_DslScriptTime = fi.LastWriteTime;
                    Calculator.LoadDsl(fi.FullName);

                    LogNoLock("[csharp] Load dsl script: " + fi.FullName);
                }
            }
            else {
                LogNoLock("[csharp] Can't find dsl script: " + fi.FullName);
            }
        }
        private static void RegisterDslScriptApi(DotnetStoryScript.DslExpression.DslCalculator calculator)
        {
            calculator.Register("outputlog", "outputlog(fmt, ...)", new DotnetStoryScript.DslExpression.ExpressionFactoryHelper<OutpuLogExp>());
            calculator.Register("writefile", "writefile(path, txt)", new DotnetStoryScript.DslExpression.ExpressionFactoryHelper<WriteFileExp>());
        }
        private static DotnetStoryScript.DslExpression.DslCalculator Calculator
        {
            get {
                if (null == s_Calculator) {
                    s_Calculator = new DotnetStoryScript.DslExpression.DslCalculator();
                    s_Calculator.Init();
                    RegisterDslScriptApi(s_Calculator);
                }
                return s_Calculator;
            }
        }

        private static string s_BasePath = string.Empty;
        private static DotnetStoryScript.DslExpression.DslCalculator? s_Calculator;
        private static DateTime s_DslScriptTime = DateTime.Now;
        private static int s_MainThreadId = 0;
        private static object s_Lock = new object();

        private static NativeApi? s_NativeApi;

        private const string c_apk_file_name_key = "apkFileName:";
        private const string c_unshell_sign_suffix = "_unshell_sign";
    }
}