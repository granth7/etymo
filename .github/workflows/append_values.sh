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
secrets:
  connectionStrings:
    existingPostgres: \"\"
"

# Append the values to the existing values.yaml file
echo "$VALUES" >> values.yaml

echo "Values appended to values.yaml"
