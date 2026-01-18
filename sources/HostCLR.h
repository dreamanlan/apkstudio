#pragma once
#include "coreclr/coreclr_delegates.h"

class QString;
extern void add_log(const QString& msg);
extern void printf_log(const char* fmt, ...);
extern int load_hostfxr(int& out_rc);
extern int load_dotnet_method(int& out_rc);

typedef void (CORECLR_DELEGATE_CALLTYPE* init_csharp_fn)(const char* base_path, void* result);
typedef int (CORECLR_DELEGATE_CALLTYPE* decompile_apk_fn)(const char* apk, const char* folder, bool java, bool res, bool smali, void* worker, void* result);
typedef int (CORECLR_DELEGATE_CALLTYPE* recompile_apk_fn)(const char* folder, void* worker, void* result);
typedef int (CORECLR_DELEGATE_CALLTYPE* sign_apk_fn)(const char* apk, const char* key, const char* keypwd, const char* alias, const char* aliaspwd, bool zipalign, void* worker, void* result);
typedef int (CORECLR_DELEGATE_CALLTYPE* install_apk_fn)(const char* apk, void* worker, void* result);
typedef int (CORECLR_DELEGATE_CALLTYPE* get_zipalign_args_fn)(const char* folder, char* args, int& args_size);

extern init_csharp_fn init_csharp_fptr;
extern decompile_apk_fn decompile_apk_fptr;
extern recompile_apk_fn recompile_apk_fptr;
extern sign_apk_fn sign_apk_fptr;
extern install_apk_fn install_apk_fptr;
extern get_zipalign_args_fn get_zipalign_args_fptr;