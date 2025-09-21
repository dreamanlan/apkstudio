#include "HostCLR.h"
#include "processutils.h"
#include "apkdecompileworker.h"
#include "apkrecompileworker.h"
#include "apksignworker.h"
#include "adbinstallworker.h"

#include <iostream>
#include <string>
#include <vector>

#if defined(_MSC_VER)
#include "windows.h"
#include "coreclr/nethost.h"
#include "coreclr/coreclr_delegates.h"
#include "coreclr/hostfxr.h"
#elif defined(__APPLE__)
#include <TargetConditionals.h>
#if TARGET_OS_OSX
#include <iostream>
#include <dlfcn.h>
#include "coreclr/nethost.h"
#include "coreclr/coreclr_delegates.h"
#include "coreclr/hostfxr.h"
#endif
#endif

void* load_library(const char* path)
{
#if defined(_MSC_VER)
    HMODULE h = ::LoadLibraryA(path);
    return reinterpret_cast<void*>(h);
#else
    return dlopen(path, RTLD_LAZY | RTLD_LOCAL);
#endif
}

void* get_export(void* h, const char* name)
{
#if defined(_MSC_VER)
    return reinterpret_cast<void*>(::GetProcAddress(reinterpret_cast<HMODULE>(h), name));
#else
    return dlsym(h, name);
#endif
}

void free_library(void* h)
{
#if defined(_MSC_VER)
    ::FreeLibrary(reinterpret_cast<HMODULE>(h));
#else
    dlclose(h);
#endif
}

void add_log(const QString& msg)
{
    ProcessOutput::instance()->emitOutputLog(msg);
}

void printf_log(const char* fmt, ...)
{
    va_list vl;
    va_start(vl, fmt);
    char buffer[4097];
    int len = vsnprintf(buffer, sizeof(buffer), fmt, vl);
    buffer[len] = '\0';
    va_end(vl);

    ProcessOutput::instance()->emitOutputLog(buffer);
}

static void convert_separators_to_platform(std::string& pathName)
{
#if _MSC_VER
    std::string::iterator it = pathName.begin(), itEnd = pathName.end();
    while (it != itEnd)
    {
        if (*it == '/')
            *it = '\\';
        ++it;
    }
#endif
}

static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;
// Function to initialize .NET Core runtime
int load_hostfxr()
{
    const char* hostfxr_path = "hostfxr.dll";
    void* lib = load_library(hostfxr_path);
    if (!lib)
    {
        printf_log("Failed to load hostfxr.dll");
        return -1;
    }

    auto init_cmdline_fptr = reinterpret_cast<hostfxr_initialize_for_dotnet_command_line_fn>(get_export(lib, "hostfxr_initialize_for_dotnet_command_line"));
    auto run_app_fptr = reinterpret_cast<hostfxr_run_app_fn>(get_export(lib, "hostfxr_run_app"));
    auto init_config_fptr = reinterpret_cast<hostfxr_initialize_for_runtime_config_fn>(get_export(lib, "hostfxr_initialize_for_runtime_config"));
    auto get_delegate_fptr = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(get_export(lib, "hostfxr_get_runtime_delegate"));
    auto close_fptr = reinterpret_cast<hostfxr_close_fn>(get_export(lib, "hostfxr_close"));

    if (!init_cmdline_fptr || !run_app_fptr || !init_config_fptr || !get_delegate_fptr || !close_fptr)
    {
        printf_log("Failed to get hostfxr functions");
        return -2;
    }

    // Initialize the .NET Core runtime
    hostfxr_initialize_parameters parameters{
        sizeof(hostfxr_initialize_parameters),
        L"./",
        L"./dotnet/Microsoft.NETCore.App/9.0.2"
    };

    hostfxr_handle cxt = nullptr;
    int rc = init_config_fptr(L"dotnetapp.runtimeconfig.json", &parameters, &cxt);
    //int argc = 1;
    //const char_t* argv[] = { L"./managed/dotnetapp.dll" };
    //int rc = init_cmdline_fptr(argc, argv, &parameters, &cxt);
    if (rc != 0 || cxt == nullptr)
    {
        printf_log("Failed to initialize .NET Core runtime");
        return -3;
    }

    // Get the delegate for the runtime
    rc = get_delegate_fptr(cxt, hdt_load_assembly_and_get_function_pointer, reinterpret_cast<void**>(&load_assembly_and_get_function_pointer));
    if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
    {
        printf_log("Failed to get load_assembly_and_get_function_pointer");
        return -4;
    }

    //run_app_fptr(cxt);

    // Close the host context
    close_fptr(cxt);

    return 0;
}

// Function pointers to call dotnet methods
init_csharp_fn init_csharp_fptr = nullptr;
decompile_apk_fn decompile_apk_fptr = nullptr;
recompile_apk_fn recompile_apk_fptr = nullptr;
sign_apk_fn sign_apk_fptr = nullptr;
install_apk_fn install_apk_fptr = nullptr;
get_zipalign_args_fn get_zipalign_args_fptr = nullptr;

// Native api
typedef void (*host_output_log_fn)(const char* msg);
typedef void (*host_show_progress_fn)(int percent, const char* msg);
typedef bool (*host_run_command_fn)(const char* cmd, const char* args, void* result);
typedef bool (*host_run_command_timeout_fn)(const char* cmd, const char* args, int timeout, void* result);
typedef int (*host_get_result_code_fn)(void* result);
typedef int (*host_get_error_count_fn)(void* result);
typedef int (*host_get_output_count_fn)(void* result);
typedef bool (*host_get_error_fn)(int index, char* path, int& path_size, void* result);
typedef bool (*host_get_output_fn)(int index, char* path, int& path_size, void* result);
typedef bool (*host_find_in_path_fn)(const char* filename, char* path, int& path_size);
typedef bool (*host_get_adb_exe_fn)(char* path, int& path_size);
typedef bool (*host_get_apktool_jar_fn)(char* path, int& path_size);
typedef bool (*host_get_jadx_exe_fn)(char* path, int& path_size);
typedef bool (*host_get_java_exe_fn)(char* path, int& path_size);
typedef bool (*host_get_uberapksigner_jar_fn)(char* path, int& path_size);
typedef bool (*host_get_zipalign_exe_fn)(char* path, int& path_size);
typedef int (*host_get_java_heap_fn)();

typedef struct {
    host_output_log_fn OutputLog;
    host_show_progress_fn ShowProgress;
    host_run_command_fn RunCommand;
    host_run_command_timeout_fn RunCommandTimeout;
    host_get_result_code_fn GetResultCode;
    host_get_error_count_fn GetErrorCount;
    host_get_output_count_fn GetOutputCount;
    host_get_error_fn GetError;
    host_get_output_fn GetOutput;
    host_find_in_path_fn FindInPath;
    host_get_adb_exe_fn GetAdbExe;
    host_get_apktool_jar_fn GetApkToolJar;
    host_get_jadx_exe_fn GetJadxExe;
    host_get_java_exe_fn GetJavaExe;
    host_get_uberapksigner_jar_fn GetUberApkSignerJar;
    host_get_zipalign_exe_fn GetZipAlignExe;
    host_get_java_heap_fn GetJavaHeap;
} HostApi;

void host_output_log(const char* msg)
{
    ProcessOutput::instance()->emitOutputLog(msg);
}
void host_show_progress(int percent, const char* msg)
{
    ProcessOutput::instance()->emitProgress(percent, msg);
}
bool host_run_command(const char* cmd, const char* args, void* result)
{
    if (!cmd || !args || !result) {
        return false;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    *res = ProcessUtils::runCommand(cmd, QString(args).split(' '));
    return res->code == 0;
}
bool host_run_command_timeout(const char* cmd, const char* args, int timeout, void* result)
{
    if (!cmd || !args || !result) {
        return false;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    *res = ProcessUtils::runCommand(cmd, QString(args).split(' '), timeout);
    return res->code == 0;
}
int host_get_result_code(void* result)
{
    if (!result) {
        return -1;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    return res->code;
}
int host_get_error_count(void* result)
{
    if (!result) {
        return 0;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    return res->error.size();
}
int host_get_output_count(void* result)
{
    if (!result) {
        return 0;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    return res->output.size();
}
bool host_get_error(int index, char* path, int& path_size, void* result)
{
    if (!path || !result) {
        return false;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    if (index < 0 || index >= res->error.size()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res->error[index].toStdString();
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_output(int index, char* path, int& path_size, void* result)
{
    if (!path || !result) {
        return false;
    }
    ProcessResult* res = reinterpret_cast<ProcessResult*>(result);
    if (index < 0 || index >= res->output.size()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res->output[index].toStdString();
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_find_in_path(const char* filename, char* path, int& path_size)
{
    if (!filename || !path) {
        return false;
    }
    QString res = ProcessUtils::findInPath(filename);
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_adb_exe(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::adbExe();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_apktool_jar(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::apktoolJar();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_jadx_exe(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::jadxExe();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_java_exe(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::javaExe();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_uber_apk_signer_jar(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::uberApkSignerJar();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
bool host_get_zipalign_exe(char* path, int& path_size)
{
    if (!path) {
        return false;
    }
    QString res = ProcessUtils::zipalignExe();
    if (res.isEmpty()) {
        path[0] = '\0';
        path_size = 0;
        return false;
    }
    std::string res_str = res.toStdString();
    convert_separators_to_platform(res_str);
    size_t len = res_str.size();
    if (len > path_size) {
        len = path_size - 1;
    }
    memcpy(path, res_str.c_str(), len);
    path[len] = '\0';
    path_size = static_cast<int>(res_str.size());
    return true;
}
int host_get_java_heap()
{
    return ProcessUtils::javaHeapSize();
}

// Function to call .NET Core method
int load_dotnet_method()
{
    const char_t* dotnet_assembly_path = L"./managed/DotnetApp.dll";
    const char_t* dotnet_class_name = L"DotNetLib.Lib, DotnetApp";

    // native api
    HostApi api;
    api.OutputLog = &host_output_log;
    api.ShowProgress = &host_show_progress;
    api.RunCommand = &host_run_command;
    api.RunCommandTimeout = &host_run_command_timeout;
    api.GetResultCode = &host_get_result_code;
    api.GetErrorCount = &host_get_error_count;
    api.GetOutputCount = &host_get_output_count;
    api.GetError = &host_get_error;
    api.GetOutput = &host_get_output;
    api.FindInPath = &host_find_in_path;
    api.GetAdbExe = &host_get_adb_exe;
    api.GetApkToolJar = &host_get_apktool_jar;
    api.GetJadxExe = &host_get_jadx_exe;
    api.GetJavaExe = &host_get_java_exe;
    api.GetUberApkSignerJar = &host_get_uber_apk_signer_jar;
    api.GetZipAlignExe = &host_get_zipalign_exe;
    api.GetJavaHeap = &host_get_java_heap;

    // For UNMANAGEDCALLERSONLY_METHOD, this must be int (or other directly copyable type), not bool.
    typedef int (CORECLR_DELEGATE_CALLTYPE* register_api_fn)(void* arg);
    register_api_fn register_api = nullptr;
    int rc = load_assembly_and_get_function_pointer(
        dotnet_assembly_path,
        dotnet_class_name,
        L"RegisterApi",
        UNMANAGEDCALLERSONLY_METHOD,
        nullptr,
        (void**)&register_api);
    if (rc || !register_api) {
        printf_log("Failure: load register_api");
    }

    if (register_api) {
        int result = register_api(&api);
        printf_log("register_api returned: %d", result);
    }

    // dotnet methods
    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"Init",
    L"DotNetLib.Lib+InitDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&init_csharp_fptr);
    if (rc || !init_csharp_fptr) {
        printf_log("Failure: load init_csharp");
    }

    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"DecompileApk",
    L"DotNetLib.Lib+DecompileApkDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&decompile_apk_fptr);
    if (rc || !decompile_apk_fptr) {
        printf_log("Failure: load decompile_apk");
    }

    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"RecompileApk",
    L"DotNetLib.Lib+RecompileApkDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&recompile_apk_fptr);
    if (rc || !recompile_apk_fptr) {
        printf_log("Failure: load recompile_apk");
    }

    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"SignApk",
    L"DotNetLib.Lib+SignApkDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&sign_apk_fptr);
    if (rc || !sign_apk_fptr) {
        printf_log("Failure: load sign_apk");
    }

    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"InstallApk",
    L"DotNetLib.Lib+InstallApkDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&install_apk_fptr);
    if (rc || !install_apk_fptr) {
        printf_log("Failure: load install_apk");
    }

    rc = load_assembly_and_get_function_pointer(
    dotnet_assembly_path,
    dotnet_class_name,
    L"GetZipAlignArgs",
    L"DotNetLib.Lib+GetZipAlignArgsDelegation, DotnetApp", // Delegate type
    nullptr,
    (void**)&get_zipalign_args_fptr);
    if (rc || !get_zipalign_args_fptr) {
        printf_log("Failure: load get_zipalign_args");
    }

    return 0;
}
