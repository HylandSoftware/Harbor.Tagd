#!/bin/bash
set -euo pipefail

SCRIPT_ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CAKE_TASK=Docker::Build
IMAGE_TAG=ci

if [ "${TRAVIS_PULL_REQUEST}" = "false" ] && [ "${TRAVIS_BRANCH}" = "master" ]; then
    CAKE_TASK=Docker::Push
    docker login -u "${DOCKER_USERNAME}" -p "${DOCKER_PASSWORD}"

    IMAGE_TAG=$(docker run -it --rm -v "$(pwd):/repo" gittools/gitversion-dotnetcore:linux-4.0.1 /repo /showvariable FullSemVer)

    # Strip Trailing newlines that gitversion generated for us
    IMAGE_TAG=${IMAGE_TAG%$'\r'}
fi

echo "Building task ${CAKE_TASK}"

"${SCRIPT_ROOT}/../build.sh" -t "${CAKE_TASK}" --tag="${IMAGE_TAG}"
