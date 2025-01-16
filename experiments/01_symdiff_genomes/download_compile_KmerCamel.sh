#! /usr/bin/env bash

set -e
set -o pipefail
set -u

PS4='\[\e[32m\][$(date "+%Y-%m-%d %H:%M:%S") L${LINENO}]\[\e[0m\] '; set -x
echo "starting"

(
	rm -vfr KmerCamel/
	git clone --recursive https://github.com/OndrejSladky/kmercamel KmerCamel
	(
		cd KmerCamel/
		git checkout 6bdbe33
		make -j 5
	)
)
ln -fs KmerCamel/kmercamel 

./kmercamel -v

echo "finished"
