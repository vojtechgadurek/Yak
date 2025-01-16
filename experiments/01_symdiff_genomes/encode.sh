#! /usr/bin/env bash

set -e
set -o pipefail
set -u

PS4='\[\e[32m\][$(date "+%Y-%m-%d %H:%M:%S") L${LINENO}]\[\e[0m\] '; set -x
echo "starting"

# Usage 
#bash encode.sh covid 500 200 TabulationFamily 31 10 3 Data/maskedSuperstring.SARS-CoV-2_PQ289255.1.k_31.S_global.fa Data/maskedSuperstring.SARS-CoV-2_PQ289254.1.k_31.S_global.fa 

#TODO:
# - how to turn off syncmers
# - how to choose which predictors are run

# Input parameters
NAME=$1
SIZE1=$2
SIZE2=$3
HASHFAMILY=$4
KMERSIZE=$5
SYNCMERLENGTH=$6
NHASHFUNCTIONS=$7
FIRSTFILE=$8
LASTFILE=$9

# Order definitions

hashfunctionnames=' '
# Create hash functions
    for i in $(seq 1 "$NHASHFUNCTIONS"); do
        hashname="hash-${i}${NAME}${SIZE1}${HASHFAMILY}"
        ./Yak "create-hash" "${hashname}" "$HASHFAMILY" "$SIZE1" "0"
        hashfunctionnames+=" ${hashname}"

    done


    for i in $(seq 1 3); do
        hashname="hash-${i}${NAME}${SIZE2}${HASHFAMILY}"
        ./Yak "create-hash" "${hashname}" "$HASHFAMILY" "$SIZE2" "0"
        hashfunctionnames+=" ${hashname}"
    done

# Create SPEC hash
./Yak "create-hash" "hash-SPEC${NAME}${SIZE2}${HASHFAMILY}" "$HASHFAMILY" "$SIZE2" "0"



hashfunctionnames+=" hash-SPEC${NAME}${SIZE2}${HASHFAMILY}"

echo "CREATED HASH FUNCS.: ${hashfunctionnames}"
# Define path for template
TEM_P="${NAME}_${SIZE1}_${SIZE2}_${HASHFAMILY}_${SYNCMERLENGTH}_${KMERSIZE}_${NHASHFUNCTIONS}"


# Create the directory for the template


mkdir "${TEM_P}"

echo "${TEM_P}"
T_P=${TEM_P}/temp${TEM_P}

# Create template
./Yak "create-template" "$T_P" "$SIZE1" "$SIZE2" $SYNCMERLENGTH $KMERSIZE $hashfunctionnames

# Encode files to sketch
./Yak "encode-to-sketch" "$T_P" "$FIRSTFILE" $TEM_P/0 2
./Yak "encode-to-sketch" "$T_P" "$LASTFILE" $TEM_P/1 2

mkdir "$TEM_P/RES"

./Yak "symmetric-difference" "$TEM_P/0/0" "$TEM_P/1/0" "$TEM_P/RES/0"
./Yak "symmetric-difference" "$TEM_P/0/1" "$TEM_P/1/1" "$TEM_P/RES/1"

./Yak "recover-sketch" "$TEM_P/RES/0" "$TEM_P/RES/recovered_0"
./Yak "recover-sketch" "$TEM_P/RES/1" "$TEM_P/RES/recovered_1"