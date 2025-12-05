### overview

The feature we're developing consists of a chatbot where in every new conversation that the user creates, they are presented with a dropdown to choose the company that we are trying to conduct our due diligence on.

It will be a pre-defined list for now, ultimately we can query some API to find out which companies are available in our sources.

When the user confirms their selection, we call an endpoint (it is asynchronous) and the user is shown a success message that tells them that we've started downloading the documents.
This is when the ingestion background process is queued.

#### How we prepare our documents for ingestion

Is it necessary to first download all the documents, and then launch the ingestion? and at the end we delete all the documents? I guess we can store them in the filesystem again in some folder is named with the conversation id and again at the end we can delete this folder.

It would actually be better if the downloading of the documents is just the first step in our sequence of steps in the ingestion pipeline.

#### How will the system know where to download documents from?

We can simply have a class that does that, which knows how to interact with the SEC EDGAR API.

#### Where to store the PDFs and other data downloaded?

We can simply store this in the local filesystem. There won't be that much data.

#### Saving time

When a user selects a company that already exists we just skip the ingestion phase.

---

### Implementation Design: Filing Downloader

#### Architecture Overview

We use **composition over inheritance** for flexibility. Each filing downloader client receives a persistence service via dependency injection, allowing us to swap storage backends (local filesystem, cloud storage, etc.) without modifying the downloaders.

```
                    ┌─────────────────────┐
                    │  IFilingDownloader  │  ← Contract: "you must download filings"
                    └─────────────────────┘
                              ▲
                              │ implements
              ┌───────────────┼───────────────┐
              │               │               │
     ┌────────┴───────┐ ┌─────┴─────┐  ┌──────┴──────┐
     │ SecEdgarClient │ │ UKClient  │  │ FutureClient│
     │                │ │ (future)  │  │  (future)   │
     └────────┬───────┘ └─────┬─────┘  └──────┬──────┘
              │               │               │
              │         uses (injected)       │
              │               │               │
              └───────────────┼───────────────┘
                              ▼
                 ┌────────────────────────┐
                 │ IFilePersistenceService│  ← Contract: "you must persist files"
                 └────────────────────────┘
                              ▲
                              │ implements
                              │
              ┌───────────────┴───────────────┐
              │                               │
   ┌──────────┴──────────┐     ┌──────────────┴──────────┐
   │LocalFilePersistence │     │ CloudFilePersistence    │
   │      Service        │     │    Service (future)     │
   └─────────────────────┘     └─────────────────────────┘
```

#### Components

1. **IFilingDownloader** — Interface defining the contract for downloading company filings

    - `DownloadCompanyFilingsAsync(List<string> filings, string companyName)`

2. **IFilePersistenceService** — Interface for file persistence (enables swapping storage backends)

    - `SaveAsync(string content, string path)`
    - `SaveAsync(byte[] content, string path)`

3. **LocalFilePersistenceService** — Concrete implementation for local filesystem storage

4. **SecEdgarClient** — Concrete downloader that knows how to interact with SEC EDGAR API

#### Why Composition?

-   **Flexibility** — Can swap `LocalFilePersistenceService` for cloud storage (Azure Blob, S3) in production
-   **Testability** — Can mock `IFilePersistenceService` in unit tests
-   **Single Responsibility** — Downloaders focus on API interaction; persistence service focuses on storage
