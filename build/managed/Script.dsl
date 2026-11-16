script(init)
{
    output_log("[dsl] init finish");
    return(0);
};

// -1 -- failed 0 -- nothing was done 1 -- finished
script(decompileapk)args($java, $res, $smali)
{
    $javaExe = native_api.GetJavaExe();
    $apktool = native_api.GetApkToolJar();
    $javaHeap = native_api.GetJavaHeap();
    if (is_null_or_empty($javaExe) || is_null_or_empty($apktool)) {
        return(-1);
    };
    native_api.ShowProgress(25, "Running apktool...");
    $args = format("-Xmx{0}m -jar {1} d{2}{3} -o {4} {5}", $javaHeap, $apktool, ($smali ? " -s" : ""), ($res ? " -r" : ""), folder, apk);
    $r = native_api.RunCommand($javaExe, $args);
    if (!$r) {
        return(-1);
    };
    if ($java) {
        native_api.ShowProgress(75, "Running jadx...");
        $jadx = native_api.GetJadxExe();
        if (is_null_or_empty($jadx)) {
            return(-1);
        };
        $args = format("-r -d {0} {1}", folder, apk);
        $r = native_api.RunCommand($jadx, $args);
        if (!$r) {
            return(-1);
        };
    };
    output_log("[dsl] decompile apk success");
    return(1);
};

script(recompileapk)args($source, $target)
{
    $javaExe = native_api.GetJavaExe();
    $apktool = native_api.GetApkToolJar();
    $zipalign = native_api.GetZipAlignExe();
    $javaHeap = native_api.GetJavaHeap();
    if (is_null_or_empty($javaExe) || is_null_or_empty($apktool) || is_null_or_empty($zipalign)) {
        return(-1);
    };
    $args = format("-Xmx{0}m -jar {1} b {2}", $javaHeap, $apktool, folder);
    $r = native_api.RunCommand($javaExe, $args);
    if (!$r) {
        output_log("build apk failed:{0}", native_api.GetResultCode());
        return(-1);
    };
    $args = format("-P 16 -f 4 {0} {1}", $source, $target);
    $r = native_api.RunCommand($zipalign, $args);
    if (!$r) {
        output_log("zipalign failed:{0}", native_api.GetResultCode());
        return(-1);
    };
    output_log("[dsl] build apk success");
    return(1);
};

script(signapk)args($key, $keypwd, $alias, $aliaspwd, $zipalign, $target)
{
    $javaExe = native_api.GetJavaExe();
    $uas = native_api.GetUberApkSignerJar();
    $zipalignExe = native_api.GetZipAlignExe();
    $javaHeap = native_api.GetJavaHeap();
    if (is_null_or_empty($javaExe) || is_null_or_empty($uas) || is_null_or_empty($zipalignExe)) {
        return(-1);
    };
    $sb = newstringbuilder();
    appendformat($sb,"-Xmx{0}m -jar {1} -a {2} --allowResign --overwrite", $javaHeap, $uas, apk);
    if (!is_null_or_empty($key) && !is_null_or_empty($alias)) {
        appendformat($sb," --ks {0} --ksPass {1} --ksAlias {2} --ksKeyPass {3} --skipZipAlign", $key, $keypwd, $alias, $aliaspwd);
    };
    $args = stringbuilder_tostring($sb);
    $r = native_api.RunCommand($javaExe, $args);
    if (!$r) {
        output_log("sign apk failed:{0}", native_api.GetResultCode());
        return(-1);
    };
    if ($zipalign) {
        $args = format("-P 16 -f 4 {0} {1}", apk, $target);
        $r = native_api.RunCommand($zipalignExe, $args);
        if (!$r) {
            output_log("zipalign failed:{0}", native_api.GetResultCode());
            return(-1);
        };
    };
    output_log("[dsl] sign apk success");
    return(1);
};

script(installapk)
{
    $adbExe = native_api.GetAdbExe();
    if (is_null_or_empty($adbExe)) {
        return(-1);
    };
    $args = format("install {0}", apk);
    $r = native_api.RunCommand($adbExe, $args);
    if (!$r) {
        output_log("install apk failed:{0}", native_api.GetResultCode());
        return(-1);
    };
    output_log("[dsl] install apk success");
    return(1);
};

script(get_zipalign_args)args($source, $target)
{
    return("-P 16 -f 4 " + $source + " " + $target);
};