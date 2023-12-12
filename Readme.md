# SyncMq gRPC Service

## Proto File (syncmq.proto)

```proto
syntax = "proto3";

option csharp_namespace = "ServiceApp";

package syncmq;

service SyncMq {
  rpc Publish (stream MessageBroker) returns (stream Respose);
  rpc Subscribe (stream Request) returns (stream MessageBroker);
}

message MessageBroker {
  string topic = 1;
  string message_id = 2;
  bytes data = 3;
  string dataAreaId = 4;
  bool message_eof = 5;
}

// The response message containing the greetings.
message Respose {
  int64 event_id = 1;
}

message Request {
  bool commit = 3;
} 