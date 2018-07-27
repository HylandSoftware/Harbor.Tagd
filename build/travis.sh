#!/bin/bash
set -euo pipefail

SCRIPT_ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CAKE_TASK=Default

echo "Building task ${CAKE_TASK}"

"${SCRIPT_ROOT}/../build.sh" -t "${CAKE_TASK}"
