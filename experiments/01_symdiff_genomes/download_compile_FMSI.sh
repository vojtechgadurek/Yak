#! /usr/bin/env bash

set -e
set -o pipefail
set -u

PS4='\[\e[32m\][$(date "+%Y-%m-%d %H:%M:%S") L${LINENO}]\[\e[0m\] '; set -x
echo "starting"

(
	rm -vfr FMSI/
	git clone --recursive https://github.com/OndrejSladky/fmsi FMSI
	(
		cd FMSI/
		git checkout f7a88dd
		make -j 5
	)
)
ln -fs FMSI/fmsi

./fmsi

echo "finished"
