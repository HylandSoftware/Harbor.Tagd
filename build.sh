#!/usr/bin/env bash
##########################################################################
# This is the Cake bootstrapper script for Linux and OS X.
# This file was based off of https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

set -euo pipefail

command -v dotnet >/dev/null 2>&1 || { 
    echo >&2 "This project requires dotnet core but it could not be found"
    echo >&2 "Please install dotnet core and ensure it is available on your PATH"
    exit 1
}

SCRIPT_ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
TOOLS_DIR=${TOOLS_DIR:-"${SCRIPT_ROOT}/tools"}
CAKE_VERSION=${CAKE_VERSION:-0.29.0}
CAKE_NETCOREAPP_VERSION=${CAKE_NETCOREAPP_VERSION:-2.0}

mkdir -p "${TOOLS_DIR}"

CAKE_DLL=$(find "${TOOLS_DIR}" -type f -name 'Cake.dll' | head -n 1)

# Define default arguments.
SCRIPT="build.cake"
TARGET="Default"
CONFIGURATION="Release"
VERBOSITY="verbose"
DRYRUN=
SHOW_VERSION=false
SCRIPT_ARGUMENTS=()

# Parse arguments.
for i in "$@"; do
    if [ "$#" -lt 2 ]; then
        break
    fi

    case $1 in
        -s|--script) SCRIPT="$2"; shift ;;
        -t|--target) TARGET="$2"; shift ;;
        -c|--configuration) CONFIGURATION="$2"; shift ;;
        -v|--verbosity) VERBOSITY="$2"; shift ;;
        -d|--dryrun) DRYRUN="-dryrun" ;;
        --version) SHOW_VERSION=true ;;
        --) shift; SCRIPT_ARGUMENTS+=("$@"); break ;;
        *) SCRIPT_ARGUMENTS+=("$1") ;;
    esac
    shift
done

###########################################################################
# INSTALL CAKE
###########################################################################

if [ ! -f "${CAKE_DLL}" ]; then
    echo "Installing Cake ${CAKE_VERSION}"

    TOOLS_PROJ="${TOOLS_DIR%/}/cake.csproj"
    echo "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>netcoreapp${CAKE_NETCOREAPP_VERSION}</TargetFramework></PropertyGroup></Project>" > "${TOOLS_PROJ}"
    dotnet add "${TOOLS_PROJ}" package Cake.CoreCLR -v "${CAKE_VERSION}" --package-directory "${TOOLS_DIR%/}/Cake.CoreCLR.${CAKE_VERSION}"

    CAKE_DLL=$(find "${TOOLS_DIR}" -type f -name 'Cake.dll' | head -n 1)

    if [ ! -f "${CAKE_DLL}" ]; then
        echo >&2 "Failed to install Cake ${CAKE_VERSION}"
        exit 1
    fi
fi

###########################################################################
# RUN BUILD SCRIPT
###########################################################################
 
# Start Cake
if $SHOW_VERSION; then
    exec dotnet "$CAKE_DLL" -version
else
    exec dotnet "$CAKE_DLL" "${SCRIPT}" "-verbosity=${VERBOSITY}" "-configuration=${CONFIGURATION}" "-target=${TARGET}" "${DRYRUN}" "${SCRIPT_ARGUMENTS[@]+\"\$\{SCRIPT_ARGUMENTS[@]\}\"}"
fi
