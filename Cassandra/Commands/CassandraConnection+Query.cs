﻿using System;

namespace Cassandra
{
    internal partial class CassandraConnection : IDisposable
    {
        public IAsyncResult BeginQuery(string cqlQuery, AsyncCallback callback, object state, object owner, ConsistencyLevel consistency, bool tracingEnabled)
        {
            return BeginJob(callback, state, owner, "QUERY", new Action<int>((streamId) =>
            {
                Evaluate(new QueryRequest(streamId, cqlQuery, consistency, tracingEnabled), streamId, new Action<ResponseFrame>((frame2) =>
                {
                    var response = FrameParser.Parse(frame2);
                    if (response is ResultResponse)
                        JobFinished(streamId, (response as ResultResponse).Output);
                    else
                        _protocolErrorHandlerAction(new ErrorActionParam() { AbstractResponse = response, StreamId = streamId });

                }));
            }));
        }

        public IOutput EndQuery(IAsyncResult result, object owner)
        {
            return AsyncResult<IOutput>.End(result, owner, "QUERY");
        }

        public IOutput Query(string cqlQuery, ConsistencyLevel consistency, bool tracingEnabled)
        {
            return EndQuery(BeginQuery(cqlQuery, null, null, this, consistency,tracingEnabled), this);
        }
    }
}
