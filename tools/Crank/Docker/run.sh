#!/usr/bin/env bash

docker run -it --name crank-agent -d --network host --restart always --privileged \
--mount type=bind,source=/home/Functions/FunctionApps/HelloApp,target=/home/Functions/FunctionApps/HelloApp \
-v /var/run/docker.sock:/var/run/docker.sock crank-agent "$@"