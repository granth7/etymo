# AppHost Aspirate

`aspirate generate` only generates configmap, deployment, and service yaml files for each service.

Any additional resources you want to apply should be added to `./aspirate-input` 
where they can then be copied into `aspirate-output/Chart/templates` 
after the `aspirate generate` step in the build workflow. 

If this isn't done, any custom resources added to `aspirate-output` will be deleted/overwritten by 
the `aspirate generate` step in the build workflow.