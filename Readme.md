# My gRPC Service

## Proto File (service.proto)

```proto
syntax = "proto3";

package mypackage;

service MyService {
  rpc MyMethod (MyRequest) returns (MyResponse);
}

message MyRequest {
  string request_data = 1;
}

message MyResponse {
  string response_data = 1;
}
