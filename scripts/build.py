import subprocess
import os
import sys
import platform
import shutil
import zipfile
import collections
import re
import json
import urllib.request
import tarfile

PLIST_VERSION_PLACEHOLDER = r"${VERSION}"

RepoPath = os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
BuildDirPath = os.path.join(RepoPath, "build")
Version: str | None = None
VMajor: int = None
VMinor: int = None
VPatch: int = None
VIdentifier: str = None
VBuild: int = None

def getOS() -> str:
    temp = platform.system().lower() # windows, darwin, linux
    if temp == "windows":
        return "win"
    elif temp == "darwin":
        return "osx"
    elif temp == "linux":
        return "linux"
    else:
        raise Exception("Platform not supported")

def getArchitecture() -> str:
    if platform.machine().lower() in ("amd64", "x86_64"):
        return "x64"
    elif platform.machine() == "arm64":
        return "arm64"
    else:
        raise Exception("Architecture not supported")

def CreateCompileTargetArgs(rid: str):
    return f"-o {os.path.join(BuildDirPath, rid)} -r {rid} -c release --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0"

OS = getOS()
Architecture = getArchitecture()
CompileTargets = {
    "win-x64": CreateCompileTargetArgs("win-x64"),
    "linux-x64": CreateCompileTargetArgs("linux-x64"),
    "osx-x64": CreateCompileTargetArgs("osx-x64"),
    "osx-arm64": CreateCompileTargetArgs("osx-arm64"),
    "portable": f"-o {os.path.join(BuildDirPath, 'portable')} -c release-portable --sc false -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
}

class cd:
    """Context manager for changing the current working directory"""
    def __init__(self, newPath):
        self.newPath = os.path.expanduser(newPath)

    def __enter__(self):
        self.savedPath = os.getcwd()
        os.chdir(self.newPath)

    def __exit__(self, etype, value, traceback):
        os.chdir(self.savedPath)


def ListFiles(dir: str = os.getcwd(), recursive = False, filter: str = None, exclude: str = None) -> list[str]:
    r = []
    for root, dirs, files in os.walk(dir):
        for name in files:
            if not filter or filter in name:
                if not exclude or exclude not in name:
                    r.append(os.path.join(root, name))
        if not recursive:
            break
    return r

def ListDirs(dir: str, recursive = False) -> list[str]:
    r = []
    for root, dirs, files in os.walk(dir):
        for name in dirs:
            r.append(os.path.join(root, name))
        if not recursive:
            break
    return r

def ParseCmds(cmds: str) -> list[str]:
    r = []
    s = ""
    firstQuote = False
    for c in cmds:
        if c == '"':
            firstQuote = not firstQuote
        elif c == ' ' and firstQuote == False:
            r.append(s)
            s = ""
        else:
            s += c
    if not s.isspace():
        r.append(s)
    return r

def ZipDirProgress(path: str, zipHandle: zipfile.ZipFile):
    for root, dir, files in os.walk(path):
        for file in files:
            print(f"Zipping {file}")
            zipHandle.write(os.path.join(root, file), os.path.relpath(os.path.join(root, file), os.path.join(path, '..')))
    print("Finished zipping")

def CopyFilesProgress(files: list[str] | str, dest: str):
    if not os.path.exists(dest) or not os.path.isdir(dest):
        os.makedirs(dest, exist_ok=True)
    if isinstance(files, str):
        print(f"Copying {files} to {dest} ...")
        shutil.copy(files, dest)
    else:
        for file in files:
            print(f"Copying {file} ...")
            shutil.copy(file, dest)
    print("Finished copying")

#Commands
def DownloadFFmpeg():
    location = os.path.join(RepoPath, "ffmpeg", OS)
    os.makedirs(location, exist_ok=True)

    if OS == "osx":
        apiurlFFmpeg = "https://evermeet.cx/ffmpeg/get/zip"
        apiurlFFprobe = "https://evermeet.cx/ffmpeg/get/ffprobe/zip"

        print("Downloading ffmpeg ...")
        urllib.request.urlretrieve(apiurlFFmpeg, os.path.join(location, "dlffmpeg.zip"))
        print("Extracting ffmpeg ...")
        with cd(location):
            with zipfile.ZipFile("dlffmpeg.zip", 'r') as zip_ref:
                zip_ref.extractall(".")
            os.remove("dlffmpeg.zip")
        
        print("Downloading ffprobe ...")
        urllib.request.urlretrieve(apiurlFFprobe, os.path.join(location, "dlffprobe.zip"))
        print("Extracting ffprobe ...")
        with cd(location):
            with zipfile.ZipFile("dlffprobe.zip", 'r') as zip_ref:
                zip_ref.extractall(".")
            os.remove("dlffprobe.zip")
        
        subprocess.call(["chmod", "-R", "777", location])
        return

    apiurl = "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest"
    with urllib.request.urlopen(apiurl) as url:
        releaseJsonData = json.loads(url.read().decode())

    assets = releaseJsonData["assets"]
    if Architecture == "x64":
        arch = "64"
    else:
        arch = Architecture

    for asset in assets:
        assetName : str = asset["name"].lower()
        if OS.lower() in assetName and arch in assetName and "-gpl" in assetName and "shared" not in assetName and "-master" in assetName:
            releaseDL : str = asset["browser_download_url"]
            break

    print(f"location: {location}")
    print(f"assetName: {assetName}")
    print(f"releaseDL: {releaseDL}")

    print("Downloading ...")
    urllib.request.urlretrieve(releaseDL, os.path.join(location, assetName))
    print("Finished downloading")

    dirName = assetName.split('.')[0]

    print("Extracting ...")
    with cd(location):
        with tarfile.open(assetName) as f:
            f.extractall('.')

    files = ListFiles(os.path.join(location, dirName, "bin"), exclude='ffplay')
    CopyFilesProgress(files, location)
    os.remove(os.path.join(location, assetName))
    shutil.rmtree(os.path.join(location, dirName))

def PrintTargetOptions():
    print()
    for compileTarget in CompileTargets:
        print(f"{compileTarget}: {CompileTargets[compileTarget]}")
    print()

def DeleteBuildDir():
    if os.path.isdir(BuildDirPath):
        shutil.rmtree(BuildDirPath)

def SetVersion(version: str):
    print(f"Setting version to: '{Version}' ...")
    global Version
    global VMajor
    global VMinor
    global VPatch
    global VIdentifier
    global VBuild
    Version = version

    #([0-9]*?)\.([0-9]*?)\.([0-9]*?)-([a-z]*?)\.([0-9]*?)$
    #([0-9]*?)\.([0-9]*?)\.([0-9]*?)$
    if '-' in version: # pre-release
        matchResult = re.search(r"([0-9]*?)\.([0-9]*?)\.([0-9]*?)-([a-z]*?)\.([0-9]*?)$", version)
        VMajor = int(matchResult.group(1))
        VMinor = int(matchResult.group(2))
        VPatch = int(matchResult.group(3))
        VIdentifier = matchResult.group(4)
        VBuild = int(matchResult.group(5))
    else: # normal release
        matchResult = re.search(r"([0-9]*?)\.([0-9]*?)\.([0-9]*?)$", version)
        VMajor = int(matchResult.group(1))
        VMinor = int(matchResult.group(2))
        VPatch = int(matchResult.group(3))
        #VIdentifier = "null"
        VBuild = 0
    for target in CompileTargets:
        CompileTargets[target] = CompileTargets[target].replace("-p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0", f"-p:AssemblyVersion={VMajor}.{VMinor}.{VPatch}.{VBuild} -p:Version={version}")

    versionLine = "public static readonly SemanticVersion Version = new(1, 0, 0);"
    if VIdentifier == None:
        newVersionLine = f"public static readonly SemanticVersion Version = new({VMajor}, {VMinor}, {VPatch});"
    else:
        newVersionLine = f"public static readonly SemanticVersion Version = new({VMajor}, {VMinor}, {VPatch}, \"{VIdentifier}\", {VBuild});"
    
    with cd(os.path.join(RepoPath, "src", "CyberPlayer.Player")):
        with open("BuildConfig.cs", 'r') as file:
            data = file.read()
            data = data.replace(versionLine, newVersionLine)
        with open("BuildConfig.cs", 'w') as file:
            file.write(data)
    with cd(RepoPath):
        with open("Info.plist", 'r') as file:
            plistData = file.read()
            plistData = re.sub(r"(?<=<key>CFBundleShortVersionString</key>\n        <string>).*?(?=</string>)", version, plistData)
        with open("Info.plist", 'w') as file:
            file.write(plistData)

def ResetVersion():
    global Version
    global VMajor
    global VMinor
    global VPatch
    global VIdentifier
    global VBuild
    Version = None
    VMajor = None
    VMinor = None
    VPatch = None
    VIdentifier = None
    VBuild = None
    for target in CompileTargets:
        CompileTargets[target] = re.sub(r'-p:AssemblyVersion=.*', '-p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0', CompileTargets[target])
    with cd(os.path.join(RepoPath, "src", "CyberPlayer.Player")):
        with open("BuildConfig.cs", 'r') as file:
            data = file.read()
            data = re.sub(r'public static readonly SemanticVersion Version = new[^;]*', 'public static readonly SemanticVersion Version = new(1, 0, 0)', data)
        with open("BuildConfig.cs", 'w') as file:
            file.write(data)
    with cd(RepoPath):
        with open("Info.plist", 'r') as file:
            plistData = file.read()
            plistData = re.sub(r"(?<=<key>CFBundleShortVersionString</key>\n        <string>).*?(?=</string>)", PLIST_VERSION_PLACEHOLDER, plistData)
        with open("Info.plist", 'w') as file:
            file.write(plistData)

def Compile(chosenTargets: str):
    if ";" in chosenTargets:
        if chosenTargets.endswith(";"):
            chosenTargets = chosenTargets[0:-1]
        chosenTargets = chosenTargets.split(";")
        for target in chosenTargets:
            os.makedirs(os.path.join(BuildDirPath, target), exist_ok=True)
            cmds = f"dotnet publish \"{os.path.join(RepoPath, 'src', 'CyberPlayer.Player', 'CyberPlayer.Player.csproj')}\" {CompileTargets[target]}"
            subprocess.call(ParseCmds(cmds))
    else:
        os.makedirs(os.path.join(BuildDirPath, chosenTargets), exist_ok=True)
        cmds = f"dotnet publish \"{os.path.join(RepoPath, 'src', 'CyberPlayer.Player', 'CyberPlayer.Player.csproj')}\" {CompileTargets[chosenTargets]}"
        subprocess.call(ParseCmds(cmds))

def CopyFFmpeg():
    for build in ListDirs(BuildDirPath):
        ffmpegPath = os.path.join(build, "ffmpeg")
        if "win" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "win")), ffmpegPath)
        elif "linux" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "linux")), ffmpegPath)
        elif "osx" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "osx")), ffmpegPath)
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "win")), ffmpegPath)
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "linux")), ffmpegPath)
            with cd(ffmpegPath):
                os.rename("ffmpeg", "ffmpeg-linux")
                os.rename("ffprobe", "ffprobe-linux")
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "ffmpeg", "osx")), ffmpegPath)
            with cd(ffmpegPath):
                os.rename("ffmpeg", "ffmpeg-osx")
                os.rename("ffprobe", "ffprobe-osx")

def CopyMediaInfo():
    for build in ListDirs(BuildDirPath):
        mediaInfoPath = os.path.join(build, "mediainfo")
        if "win" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "mediainfo", "win")), mediaInfoPath)
        elif "linux" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "mediainfo", "linux")), mediaInfoPath)
        elif "osx" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(RepoPath, "mediainfo", "osx")), mediaInfoPath)

def RemovePDBs():
    for file in ListFiles(BuildDirPath, True):
        if file.endswith(".pdb"):
            os.remove(file)
        elif file.endswith(".dbg"):
            os.remove(file)
    for dir in ListDirs(BuildDirPath, True):
        if dir.endswith(".dsym"):
            shutil.rmtree(dir)

#Make lib dir courtesy of https://github.com/nulastudio/NetBeauty2
def MakeLibraryDir(chosenTargets: str):
    if chosenTargets == "all":
        for compileTarget in CompileTargets:
            if compileTarget == "sc" or compileTarget == "all":
                continue
            if "--sc true" in CompileTargets[compileTarget]:
                    subprocess.call(f"nbeauty2 --usepatch --loglevel Detail --hiddens \"hostfxr;hostpolicy;*.deps.json;*.runtimeconfig*.json\" {os.path.join(BuildDirPath, compileTarget)} lib \"libmpv-2.dll;\"")
            else:
                subprocess.call(f"nbeauty2 --loglevel Detail {os.path.join(BuildDirPath, compileTarget)} lib \"libmpv-2.dll;\"")
    elif ";" in chosenTargets:
        chosenTargets = chosenTargets.split(";")
        if "all" in chosenTargets:
            raise Exception("All is not valid when using multiple compile targets")
        if "sc" in chosenTargets:
            chosenTargets.remove("sc")
            for target in CompileTargets:
                if "--sc true" in CompileTargets[target] and target not in chosenTargets:
                    chosenTargets.append(target)
        for target in chosenTargets:
            if "--sc true" in CompileTargets[target]:
                subprocess.call(f"nbeauty2 --usepatch --loglevel Detail --hiddens \"hostfxr;hostpolicy;*.deps.json;*.runtimeconfig*.json\" {os.path.join(BuildDirPath, target)} lib \"libmpv-2.dll;\"")
            else:
                subprocess.call(f"nbeauty2 --loglevel Detail {os.path.join(BuildDirPath, target)} lib \"libmpv-2.dll;\"")
    else:
        if "--sc true" in CompileTargets[chosenTargets]:
                subprocess.call(f"nbeauty2 --usepatch --loglevel Detail --hiddens \"hostfxr;hostpolicy;*.deps.json;*.runtimeconfig*.json\" {os.path.join(BuildDirPath, chosenTargets)} lib \"libmpv-2.dll;\"")
        else:
            subprocess.call(f"nbeauty2 --loglevel Detail {os.path.join(BuildDirPath, chosenTargets)} lib \"libmpv-2.dll;\"")

#Copy licenses, readme, etc.
def CopyMDs():
    mdFiles = ListFiles(RepoPath, filter=".md")
    for build in ListDirs(BuildDirPath):
        CopyFilesProgress(mdFiles, build)

#Copy updater
def CopyUpdater():
    updaterBuildPath = os.path.join(RepoPath, "cyber-lib", "build")
    for build in ListDirs(BuildDirPath):
        if "win-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'win-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "linux-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'linux-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "osx-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'osx-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "osx-arm64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'osx-arm64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'portable')}", exclude=".pdb"), os.path.join(build, "updater"))

#Zip
def ZipBuilds():
    with cd(BuildDirPath):
        for build in ListDirs(BuildDirPath):
            print(f"Zipping {build} ...")
            shutil.make_archive(build, "zip", build)

def DeleteBinReleaseDirs():
    dirs = ListDirs(os.path.join(RepoPath, "src", "CyberPlayer.Player", "bin"))
    for dir in dirs:
        if "release" in os.path.basename(dir):
            shutil.rmtree(dir)

def DeleteBuildDirs():
    for build in ListDirs(BuildDirPath):
        shutil.rmtree(build)

def CopyMpvLib():
    for build in ListDirs(BuildDirPath):
        if "win" in os.path.basename(build):
            CopyFilesProgress(os.path.join(RepoPath, "mpv", "win", "libmpv-2.dll"), build)
        elif "linux" in os.path.basename(build):
            CopyFilesProgress(os.path.join(RepoPath, "mpv", "linux-2.1.0", "libmpv.so.2"), build)
        elif "osx-arm64" in os.path.basename(build):
            CopyFilesProgress(os.path.join(RepoPath, "mpv", "osx-arm64-2.1.0", "libmpv.2.dylib"), build)
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(os.path.join(RepoPath, "mpv", "win", "libmpv-2.dll"), build)
            CopyFilesProgress(os.path.join(RepoPath, "mpv", "linux-2.1.0", "libmpv.so.2"), build)
            #CopyFilesProgress(os.path.join(RepoPath, "mpv", "osx-2.1.0", "libmpv.2.dylib"), build)

# Call after Compile
def BuildUpdater():
    csproj = os.path.join(RepoPath, "cyber-lib", "UpdaterAvalonia", "UpdaterAvalonia.csproj")
    buildDir = os.path.join(RepoPath, "cyber-lib", "build")
    for build in ListDirs(BuildDirPath):
        if "win-x64" in os.path.basename(build):
            subprocess.call(["dotnet", "publish", csproj, "-o", os.path.join(buildDir, "win-x64"), "-r", "win-x64", "-p:PublishSingleFile=true", "-p:PublishTrimmed=true", "-c", "release", "--sc", "true"])
        elif "linux-x64" in os.path.basename(build):
            subprocess.call(["dotnet", "publish", csproj, "-o", os.path.join(buildDir, "linux-x64"), "-r", "linux-x64", "-p:PublishSingleFile=true", "-p:PublishTrimmed=true", "-c", "release", "--sc", "true"])
        elif "osx-x64" in os.path.basename(build):
            subprocess.call(["dotnet", "publish", csproj, "-o", os.path.join(buildDir, "osx-x64"), "-r", "osx-x64", "-p:PublishSingleFile=true", "-p:PublishTrimmed=true", "-c", "release", "--sc", "true"])
        elif "osx-arm64" in os.path.basename(build):
            subprocess.call(["dotnet", "publish", csproj, "-o", os.path.join(buildDir, "osx-arm64"), "-r", "osx-arm64", "-p:PublishSingleFile=true", "-p:PublishTrimmed=true", "-c", "release", "--sc", "true"])

# Call after specifying version or default of 1.0.0 will be used
def CreateWindowsInstaller():
    setupScriptPath = os.path.join(RepoPath, "scripts", "win-setup.iss")
    if Version == None:
        subprocess.call(["iscc", setupScriptPath])
    else:
        subprocess.call(["iscc", f"-DMyAppVersion={VMajor}.{VMinor}.{VPatch}.{VBuild}", setupScriptPath])

Command = collections.namedtuple('Command', ['description', 'function', 'hasParam'])

Commands = {
    "del": Command("Deletes the build directory", DeleteBuildDir, False),
    "delbinrel": Command("Deletes the dirs with 'release' in them in the bin folder", DeleteBinReleaseDirs, False),
    "delbuilddirs": Command("Deletes the dirs in the build folder", DeleteBuildDirs, False),
    "version": Command("Set the version number when compiling", SetVersion, "Enter version number: "), #version arg
    "resetversion": Command("Resets the version to 1.0.0.0", ResetVersion, False),
    "compile": Command("Compiles for the target platform", Compile, "Enter a compile target: "), #compiletarget arg
    "buildupdater" : Command("Calls the updater build script", BuildUpdater, False),
    "rmpdbs": Command("Remove all pdb files", RemovePDBs, False),
    "lib": Command("Makes a library directory for dlls", MakeLibraryDir, "Enter a compile target: "), #compiletarget/s arg
    "dlffmpeg": Command("Downloads ffmpeg binaries from their recommended sources", DownloadFFmpeg, False),
    "cpymds": Command("Copy all markdown files from working directory", CopyMDs, False),
    "cpyupdater": Command("Copy updater to each build", CopyUpdater, False),
    "cpyffmpeg": Command("Copy ffmpeg executables to builds", CopyFFmpeg, False),
    "cpympv": Command("Copy libmpv to builds", CopyMpvLib, False),
    "cpymediainfo": Command("Copy mediainfo executables to builds", CopyMediaInfo, False),
    "zip": Command("Zip each build", ZipBuilds, False),
    "winpkg": Command("Creates a windows installer with innosetup", CreateWindowsInstaller, False)
}

def ParseArgs(args: list[str]) -> list[Command]:
    result = []
    for x in range(0, len(args)):
        if args[x].startswith('-'):
            if args[x][1:] in Commands:
                newCommand = Commands[args[x][1:]]
                if newCommand.hasParam == False:
                    result.append(newCommand)
                else:
                    newCommand = Command(None, newCommand.function, args[x + 1])
                    result.append(newCommand)
            else:
                raise Exception(f"{args[x]} is not a command")
    return result

def Main(args: list[str]):
    if len(args) > 0:
        #call each command with params from args
        callCommands = ParseArgs(args)
        for callCommand in callCommands:
            if callCommand.hasParam == False:
                callCommand.function()
            else:
                callCommand.function(callCommand.hasParam)
        return

    userInput = None
    while(userInput != "exit"):
        for command in Commands:
            print(f"{command}: {Commands[command].description}")
        userInput = input()
        if userInput in Commands:
            if Commands[userInput].hasParam == False:
                Commands[userInput].function()
            else:
                if userInput == "compile" or userInput == "lib":
                    PrintTargetOptions()
                param = input(Commands[userInput].hasParam)
                Commands[userInput].function(param)

if __name__ == "__main__":
    Main(sys.argv[1:])
