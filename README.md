
# Overview of projects

Xyz.Specs: Contains the unit and integration tests of the Xyz project

Xyz.Contracts: Contains the command and event classes of the Xyz project. The Contract projects are
               projects with minimal dependencies that can be used to call the API and integrate with
               the Xyz project in a strongly typed way. They usually contain a XyzApiClient class
               which is a slim wrapper around the WebAPI and takes care of correct JSON serialization
               and deserialization.

DX.Contracts: Contains  (1) basic classes needed by the Xyz.Contracts projects and (2) contains most   
              of the JSON.NET serialization logic.

CQRS: Contains the generic application framework and a reference CQRS implementation.

CQRS.Codegen: A Microsoft.CodeAnalysis projects the generates code for the event and command classes.
