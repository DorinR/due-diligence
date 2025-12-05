Concrete things we are trying to avoid:
What are some problems we could encounter?

During the ingestion pipeline there could be an error during one of the steps (parsing, chunking, embedding generation, etc) so
perhaps we don't want to have to just show the user an error message and make them start from the beginning.

There are a few things that we would like to avoid:

1. Just wasting time - generating embeddings, persisting a ton of embeddings takes
   time, so if there is a failure somewhere and then the process restarts from the beginning
   it would take a long time. If we do some work, we should in theory be able to somehow persist it.
2. Wasting money - imagine you just generated embeddings for 1000 pdf pages, and when your system
   has to persist it there is a failure with the db. We would now have to re-generate those embeddings.

We can solve both of these issues by adding some durability to our ingestion pipeline.

So we can have a process with these steps:

0. Download all required files.
1. Extract text from pdf, word, graphs, etc -> text
2. Normalizing, cleaning text -> cleaned text
3. Chunking -> chunks
4. Embed -> Embeddings
5. Persist -> Persisted embeddings

So at a high level we have each of these steps running, and they are configured with an exponential backoff retry.
And when each step completes, it persists its data on the local disk for now (but normally this would be in a proper object storage like s3 or equivalent) this is what makes it durable. We also have a table that tracks the status of a given job.

### Writing retry-able jobs

Jobs should be idempotent. Meaning that the outcome is always the same no matter how many times you call it.
For the first three steps this is pretty straightfoward. We would only need to make sure that we do a sort of
upsert when persisting the data to local disk (that way we don't end up with duplicated data being persisted). Even if we have to retry these steps they are quick operations and we won't lose time from retries.
For step 0 its a bit trickier, the retries should respect rate limits. And we should also check that we don't already have a given file when we're doing a retry.
For step 4 it is a bit more nuanuced. Since we may be doing a large embedding generation run, ideally we persist them to localStorage immediately after getting them from our llm-provider? Like we almost stream them to the local disk instead of accumulating them all in memory and then persisting them all to local disk in one shot? And here again we do an upsert. The Load partial embedding step is neat. that way we really minimize losses in the event of a failure.
For step 5 this would be the same thing, but we would also have to make sure to do an upsert when persisting embeddings to the database.

### Atomic writes to disk

Let's say we've just extracted the text from our 300 page PDF and are now persiting it to local disk to end our
job. If we start writing to a filepath and are interrupted in the process - the data could be corrupt and unreadable.
The way to protect against this is to use the fact that rename operation are atomic in linux and macos systems.
So you can initiate your write to disk at a path like `desiredpath.tmp`, and when that completes successfully,
we simply do a rename. This is guaranteed to be atomic, and so either it fails and when we retry the system will just have to re-run the job, or it will succeed.

#### Cleanup considerations

Periodically, it would be good to delete any potential .tmp files that were generated to support the ingestion job.
We could for example have a final job that runs after the persistence job completes in order to delete any existing .tmp files and also deletes all of the files created in the filesystem.

#### Where we store the temporary artifacts of the ingestion pileine

```
/data/ingestion-jobs/
└── {conversationId}/
    ├── raw/                          # Step 0: Downloaded documents
    │   ├── 10-K_0000320193-23-000077.htm
    │   ├── 10-Q_0000320193-23-000064.htm
    │   └── 8-K_0000320193-23-000058.htm
    │
    ├── extracted/                    # Step 1: Extracted text
    │   ├── 10-K_0000320193-23-000077.txt
    │   ├── 10-Q_0000320193-23-000064.txt
    │   └── 8-K_0000320193-23-000058.txt
    │
    ├── cleaned/                      # Step 2: Normalized text
    │   └── ...
    │
    ├── chunks/                       # Step 3: Chunked text
    │   └── chunks.json
    │
    ├── embeddings/                   # Step 4: Generated embeddings
    │   └── embeddings.json           # (or streamed partial files)
    │
    └── status.json                   # Pipeline state tracking
```
