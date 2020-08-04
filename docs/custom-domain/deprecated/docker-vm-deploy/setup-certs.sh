#!/bin/bash

CERT_FINGERPRINT=$1
CA_CERT_URL=$2

CERT_FINGERPRINT=`echo $CERT_FINGERPRINT | tr [a-z] [A-Z]`

if [ ! -z "$CA_CERT_URL" ]; then

    curl $CA_CERT_URL -o ca_cert.crt

    set +e
    certDetails=`openssl x509 -in ca_cert.crt -text -noout`
    set -e

    # if it is not PEM, it must be DER
    if [ -z "$certDetails" ]; then
        openssl x509 -in ca_cert.crt -inform der -outform pem -out ca_cert_pem.crt
    else
        mv ca_cert.crt ca_cert_pem.crt
    fi

    sudo cat "/var/lib/waagent/$CERT_FINGERPRINT.crt" ca_cert_pem.crt > cert.crt
    export CERT_LOCATION=`pwd`/cert.crt

else
    export CERT_LOCATION=/var/lib/waagent/${CERT_FINGERPRINT}.crt
fi

export PRV_LOCATION=/var/lib/waagent/${CERT_FINGERPRINT}.prv
