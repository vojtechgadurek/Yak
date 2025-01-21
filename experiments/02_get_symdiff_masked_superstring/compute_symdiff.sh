#! /usr/bin/env bash

set -e
set -o pipefail
set -u

PS4='\[\e[32m\][$(date "+%Y-%m-%d %H:%M:%S") L${LINENO}]\[\e[0m\] '; set -x

# USAGE: ./compute_symdiff.sh <FASTA with 1st masked superstring> <FASTA 2nd masked superstring> <symdiff intermediate FASTA> <output FASTA file>

MS_FILE1=$1 
MS_FILE2=$2
SYMDIFF_FILE=$3
OUT_FILE=$4
K=31 # currently only 31

./fmsi index -k $K "$MS_FILE1"
./fmsi index -k $K "$MS_FILE2"
./fmsi symdiff -k $K -p "$MS_FILE1" -p "$MS_FILE2" -r "$SYMDIFF_FILE"
./fmsi export $SYMDIFF_FILE >"$OUT_FILE"

echo "finished"
