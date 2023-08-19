import subprocess
import os
import sys
import shutil
import zipfile
import collections
import tqdm
import re

CompileTargets = {
    "win-x64-multi": f"-o \"{os.path.join('build', 'win-x64-multi')}\" -r win-x64 -c release-win-x64-multi --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "win-x64-single": f"-o \"{os.path.join('build', 'win-x64-single')}\" -r win-x64 -c release-win-x64-single --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "linux-x64-multi": f"-o \"{os.path.join('build', 'linux-x64-multi')}\" -r linux-x64 -c release-linux-x64-multi --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "linux-x64-single": f"-o \"{os.path.join('build', 'linux-x64-single')}\" -r linux-x64 -c release-linux-x64-single --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "osx-x64-multi": f"-o \"{os.path.join('build', 'osx-x64-multi')}\" -r osx-x64 -c release-osx-x64-multi --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "osx-x64-single": f"-o \"{os.path.join('build', 'osx-x64-single')}\" -r osx-x64 -c release-osx-x64-single --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "osx.13-arm64-multi": f"-o \"{os.path.join('build', 'osx.13-arm64-multi')}\" -r osx.13-arm64 -c release-osx-x64-multi --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "osx.13-arm64-single": f"-o \"{os.path.join('build', 'osx.13-arm64-single')}\" -r osx.13-arm64 -c release-osx-x64-single --sc true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "portable-multi": f"-o \"{os.path.join('build', 'portable-multi')}\" -c release-portable-multi --sc false -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "portable-single": f"-o \"{os.path.join('build', 'portable-single')}\" -c release-portable-single --sc false -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "win-x64-installer": f"-o \"{os.path.join('build', 'win-x64-installer')}\" -r win-x64 -c release-win-x64-single --sc false -p:PublishReadyToRun=true -p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0",
    "sc": "all self contained",
    "all": "all of the above"
}

BuildDirPath = os.path.join(os.getcwd(), "build")

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
            s += c
        elif c == ' ' and firstQuote == False:
            r.append(s)
            s = ""
        else:
            s += c
    return r

def ZipDirProgress(path: str, zipHandle: zipfile.ZipFile):
    for root, dir, files in os.walk(path):
        for file in tqdm.tqdm(files):
            zipHandle.write(os.path.join(root, file), os.path.relpath(os.path.join(root, file), os.path.join(path, '..')))
    print("Finished")

def CopyFilesProgress(files: list[str] | str, dest: str):
    if not os.path.exists(dest) or not os.path.isdir(dest):
        os.mkdir(dest)
    if isinstance(files, str):
        print(f"Copying {files} to {dest}")
        shutil.copy(files, dest)
    else:
        for file in tqdm.tqdm(files):
            shutil.copy(file, dest)
    print("Finished")

#Commands
def PrintTargetOptions():
    print()
    for compileTarget in CompileTargets:
        print(f"{compileTarget}: {CompileTargets[compileTarget]}")
    print()

def DeleteBuildDir():
    if os.path.isdir(BuildDirPath):
        shutil.rmtree(BuildDirPath)

def SetVersion(version: str):
    for target in CompileTargets:
        CompileTargets[target] = CompileTargets[target].replace("-p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0", f"-p:AssemblyVersion={version} -p:Version={version}")
    versionArray = version.split('.')
    if len(versionArray) < 2 or len(versionArray) > 4:
        raise Exception("Version must contain 2-4 places (major, minor, build, revision)")
    newVersion = ""
    versionLine = "public static readonly Version Version = new(1, 0, 0, 0);"
    for v in versionArray:
        newVersion += v + ', '
    newVersion = newVersion[:-2]
    newVersionLine = f"public static readonly Version Version = new({newVersion});"
    with cd(os.path.join(os.getcwd(), "src", "CyberPlayer.Player")):
        with open("BuildConfig.cs", 'r') as file:
            data = file.read()
            data = data.replace(versionLine, newVersionLine)
        with open("BuildConfig.cs", 'w') as file:
            file.write(data)

def ResetVersion():
    for target in CompileTargets:
        CompileTargets[target] = re.sub(r'-p:AssemblyVersion=.*', '-p:AssemblyVersion=1.0.0.0 -p:Version=1.0.0.0', CompileTargets[target])
    with cd(os.path.join(os.getcwd(), "src", "CyberPlayer.Player")):
        with open("BuildConfig.cs", 'r') as file:
            data = file.read()
            data = re.sub(r'public static readonly Version Version = new[^;]*', 'public static readonly Version Version = new(1, 0, 0, 0)', data)
        with open("BuildConfig.cs", 'w') as file:
            file.write(data)

def Compile(chosenTargets: str):
    if chosenTargets == "all":
        for compileTarget in CompileTargets:
            if compileTarget == "sc" or compileTarget == "all":
                continue
            cmds = f"dotnet publish \"{os.path.join('src', 'cyberplayer.player', 'cyberplayer.player.csproj')}\" {CompileTargets[compileTarget]}"
            subprocess.call(ParseCmds(cmds))
    elif ";" in chosenTargets:
        if chosenTargets.endswith(";"):
            chosenTargets = chosenTargets[0:-1]
        chosenTargets = chosenTargets.split(";")
        if "all" in chosenTargets:
            raise Exception("All is not valid when using multiple compile targets")
        if "sc" in chosenTargets:
            chosenTargets.remove("sc")
            for target in CompileTargets:
                if "--sc true" in CompileTargets[target] and target not in chosenTargets:
                    chosenTargets.append(target)
        for target in chosenTargets:
            cmds = f"dotnet publish \"{os.path.join('src', 'cyberplayer.player', 'cyberplayer.player.csproj')}\" {CompileTargets[target]}"
            subprocess.call(ParseCmds(cmds))
    else:
        cmds = f"dotnet publish \"{os.path.join('src', 'cyberplayer.player', 'cyberplayer.player.csproj')}\" {CompileTargets[chosenTargets]}"
        subprocess.call(ParseCmds(cmds))

def CopyFFmpeg():
    for build in ListDirs(BuildDirPath):
        ffmpegPath = os.path.join(build, "ffmpeg")
        if "win" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "win")), ffmpegPath)
        elif "linux" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "linux")), ffmpegPath)
        elif "osx" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "osx")), ffmpegPath)
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "win")), ffmpegPath)
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "linux")), ffmpegPath)
            with cd(ffmpegPath):
                os.rename("ffmpeg", "ffmpeg-linux")
                os.rename("ffprobe", "ffprobe-linux")
            CopyFilesProgress(ListFiles(os.path.join(os.getcwd(), "ffmpeg", "osx")), ffmpegPath)
            with cd(ffmpegPath):
                os.rename("ffmpeg", "ffmpeg-osx")
                os.rename("ffprobe", "ffprobe-osx")

def RemovePDBs():
    for file in ListFiles(BuildDirPath, True):
        if file.endswith(".pdb"):
            os.remove(file)

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
    mdFiles = ListFiles(filter=".md")
    for build in ListDirs(BuildDirPath):
        CopyFilesProgress(mdFiles, build)

#Copy updater
def CopyUpdater(updaterBuildPath: str):
    for build in ListDirs(BuildDirPath):
        if "win-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'win-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "linux-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'linux-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "osx-x64" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'osx-x64')}", exclude=".pdb"), os.path.join(build, "updater"))
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(ListFiles(f"{os.path.join(updaterBuildPath, 'portable')}", exclude=".pdb"), os.path.join(build, "updater"))

#Zip
def ZipBuilds():
    with cd(BuildDirPath):
        for build in ListDirs(BuildDirPath):
            print(f"Zipping {build}")
            cmds = f"7z a -tzip {os.path.abspath(build)}.zip {build}"
            subprocess.call(ParseCmds(cmds))

def DeleteBinReleaseDirs():
    dirs = ListDirs(os.path.join(os.getcwd(), "src", "CyberPlayer.Player", "bin"))
    for dir in dirs:
        if "release" in os.path.basename(dir):
            shutil.rmtree(dir)

def DeleteBuildDirs():
    for build in ListDirs(BuildDirPath):
        shutil.rmtree(build)

def CopyMpvLib():
    for build in ListDirs(BuildDirPath):
        if "win" in os.path.basename(build):
            CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "win", "libmpv-2.dll"), build)
        elif "linux" in os.path.basename(build):
            CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "linux-2.1.0", "libmpv.so.2"), build)
        elif "osx.13-arm64" in os.path.basename(build):
            CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "osx-arm64-2.1.0", "libmpv.2.dylib"), build)
        elif "portable" in os.path.basename(build):
            CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "win", "libmpv-2.dll"), build)
            CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "linux-2.1.0", "libmpv.so.2"), build)
            #CopyFilesProgress(os.path.join(os.getcwd(), "mpv", "osx-2.1.0", "libmpv.2.dylib"), build)

def BuildUpdater():
    with cd(os.path.join(os.getcwd(), 'cyber-lib')):
        subprocess.call("py build.py")

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
    "cpymds": Command("Copy all markdown files from working directory", CopyMDs, False),
    "cpyupdater": Command("Copy updater to each build", CopyUpdater, "Enter the path to the build dir of updater: "), #updaterbuildpath arg
    "cpyffmpeg": Command("Copy ffmpeg executables to builds", CopyFFmpeg, False),
    "cpympv": Command("Copy libmpv to builds", CopyMpvLib, False),
    "zip": Command("Zip each build", ZipBuilds, False)
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

Main(sys.argv[1:])
