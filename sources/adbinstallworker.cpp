#include <QDebug>
#include "adbinstallworker.h"
#include "processutils.h"
#include "HostCLR.h"

AdbInstallWorker::AdbInstallWorker(const QString &apk, QObject *parent)
    : QObject(parent), m_Apk(apk)
{
}

void AdbInstallWorker::install()
{
    emit started();
#ifdef QT_DEBUG
    qDebug() << "Installing" << m_Apk;
#endif
    ProcessResult result{};
    if (install_apk_fptr) {
        std::string s = m_Apk.toStdString();
        int r = install_apk_fptr(s.c_str(), this, &result);
#ifdef QT_DEBUG
        qDebug() << "CSharp install apk returned code" << r;
#endif
        if (r > 0) {
            emit installFinished(m_Apk);
            emit finished();
            return;
        }
        else if (r < 0) {
            emit installFailed(m_Apk);
            return;
        }
    }
    const QString adb = ProcessUtils::adbExe();
    if (adb.isEmpty()) {
        emit installFailed(m_Apk);
        return;
    }
    QStringList args;
    args << "install" << m_Apk;
    result = ProcessUtils::runCommand(adb, args);
#ifdef QT_DEBUG
    qDebug() << "ADB returned code" << result.code;
#endif
    if (result.code != 0) {
        emit installFailed(m_Apk);
        return;
    }
    emit installFinished(m_Apk);
    emit finished();
}
