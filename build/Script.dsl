script(init)
{
    outputlog("[dsl] init finish");
    return(0);
};

// -1 -- failed 0 -- nothing was done 1 -- finished
script(decompileapk)args($java, $res, $smali)
{
    $javaExe = nativeapi.GetJavaExe();
    $apktool = nativeapi.GetApkToolJar();
    $javaHeap = nativeapi.GetJavaHeap();
    if (isnullorempty($javaExe) || isnullorempty($apktool)) {
        return(-1);
    };
    nativeapi.ShowProgress(25, "Running apktool...");
    $args = format("-Xmx{0}m -jar {1} d{2}{3} -o {4} {5}", $javaHeap, $apktool, ($smali ? " -s" : ""), ($res ? " -r" : ""), folder, apk);
    $r = nativeapi.RunCommand($javaExe, $args);
    if (!$r) {
        return(-1);
    };
    if ($java) {
        nativeapi.ShowProgress(75, "Running jadx...");
        $jadx = nativeapi.GetJadxExe();
        if (isnullorempty($jadx)) {
            return(-1);
        };
        $args = format("-r -d {0} {1}", folder, apk);
        $r = nativeapi.RunCommand($jadx, $args);
        if (!$r) {
            return(-1);
        };
    };
    outputlog("[dsl] decompile apk success");
    return(1);
};

script(recompileapk)args($source, $target)
{
    $javaExe = nativeapi.GetJavaExe();
    $apktool = nativeapi.GetApkToolJar();
    $zipalign = nativeapi.GetZipAlignExe();
    $javaHeap = nativeapi.GetJavaHeap();
    if (isnullorempty($javaExe) || isnullorempty($apktool) || isnullorempty($zipalign)) {
        return(-1);
    };
    $args = format("-Xmx{0}m -jar {1} b {2}", $javaHeap, $apktool, folder);
    $r = nativeapi.RunCommand($javaExe, $args);
    if (!$r) {
        outputlog("build apk failed:{0}", nativeapi.GetResultCode());
        return(-1);
    };
    $args = format("-P 16 -f 4 {0} {1}", $source, $target);
    $r = nativeapi.RunCommand($zipalign, $args);
    if (!$r) {
        outputlog("zipalign failed:{0}", nativeapi.GetResultCode());
        return(-1);
    };
    outputlog("[dsl] build apk success");
    return(1);
};

script(signapk)args($key, $keypwd, $alias, $aliaspwd, $zipalign, $target)
{
    $javaExe = nativeapi.GetJavaExe();
    $uas = nativeapi.GetUberApkSignerJar();
    $zipalignExe = nativeapi.GetZipAlignExe();
    $javaHeap = nativeapi.GetJavaHeap();
    if (isnullorempty($javaExe) || isnullorempty($uas) || isnullorempty($zipalignExe)) {
        return(-1);
    };
    $sb = newstringbuilder();
    appendformat($sb,"-Xmx{0}m -jar {1} -a {2} --allowResign --overwrite", $javaHeap, $uas, apk);
    if (!isnullorempty($key) && !isnullorempty($alias)) {
        appendformat($sb," --ks {0} --ksPass {1} --ksAlias {2} --ksKeyPass {3} --skipZipAlign", $key, $keypwd, $alias, $aliaspwd);
    };
    $args = stringbuilder_tostring($sb);
    $r = nativeapi.RunCommand($javaExe, $args);
    if (!$r) {
        outputlog("sign apk failed:{0}", nativeapi.GetResultCode());
        return(-1);
    };
    if ($zipalign) {
        $args = format("-P 16 -f 4 {0} {1}", apk, $target);
        $r = nativeapi.RunCommand($zipalignExe, $args);
        if (!$r) {
            outputlog("zipalign failed:{0}", nativeapi.GetResultCode());
            return(-1);
        };
    };
    outputlog("[dsl] sign apk success");
    return(1);
};

script(installapk)
{
    $adbExe = nativeapi.GetAdbExe();
    if (isnullorempty($adbExe)) {
        return(-1);
    };
    $args = format("install {0}", apk);
    $r = nativeapi.RunCommand($adbExe, $args);
    if (!$r) {
        outputlog("install apk failed:{0}", nativeapi.GetResultCode());
        return(-1);
    };
    outputlog("[dsl] install apk success");
    return(1);
};

script(get_zipalign_args)args($source, $target)
{
    return("-P 16 -f 4 " + $source + " " + $target);
};