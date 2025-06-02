# Etymo

An open source flashcard tool for learning. 
Features include:
- Browse popular flashcard sets created by the community.
- Search by topic with tags.
- Create an account or sign in with Google to manage private flashcard sets. 
- Upvote public flashcard sets to show appreciation.
- Play a matching game to test your knowledge.
- Report inappropriate content for moderators to review and resolve.
- Admin tools to edit and delete content.

![etymo home page](images/etymo-home.png?raw=true "Title")

An example game:
![etymo home page](images/etymo-game.png?raw=true "Title")

### Helm install
Add the etymo helm repo:

`helm repo add etymo https://granth7.github.io/etymo`

Then install:

`helm install etymo https://granth7.github.io/etymo`

### Flux install:
The basic helm install uses default values that need to be configured.
If you are using [Flux](https://github.com/fluxcd/flux2) with Kubernetes, you can copy my configuration from
[here](https://github.com/granth7/talos-flux-cluster/tree/main/kubernetes/apps/default/etymo). Make sure to reference the helm repository
by adding the following HelmRepository in `/kubernetes/flux/repositories/helm/`

`etymo.yaml`
```
---
apiVersion: source.toolkit.fluxcd.io/v1
kind: HelmRepository
metadata:
  name: etymo
  namespace: flux-system
spec:
  interval: 2h
  url: https://granth7.github.io/etymo
```

### Demo
A live demo can be found at https://etymo.hender.tech/
