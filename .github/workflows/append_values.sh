#!/bin/bash

# Define the values to append
VALUES="
expose:
  webfrontend:
    ingress:
      enabled: false
      ingressClassName: internal
      annotations: {}
      labels: {}
      path: /
      pathType: Prefix
      hosts: [\"etymo.your.domain\"]
  keycloak:
    ingress:
      enabled: false
      ingressClassName: internal
      annotations: {}
      labels: {}
      path: /
      pathType: Prefix
      hosts: [\"sso.etymo.your.domain\"]
      tls:
        - hosts: \"sso.etymo.your.domain\"
          secretName: \"tls.secret\"
secrets:
  connectionStrings:
    existingPostgres: \"secret-name/secret-key\"
"

# Append the values to the existing values.yaml file
echo "$VALUES" >> values.yaml

echo "Values appended to values.yaml"
