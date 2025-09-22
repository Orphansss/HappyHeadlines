# HappyHeadlines

## Comment Service


### Fault Isolation: Retry and Circuit Breaker Patterns in CommentService using Polly

The `CommentService` depends on the `ProfanityService` to filter comment text before storing it. We've implemented a circuit breaker pattern into the CommentService to take over if the ProfanityService is no longer available.

#### Policies with Polly
We use [Polly](https://github.com/App-vNext/Polly) to protect calls:

1. **Retry Policy**
    - Retries up to 3 times with exponential backoff (200ms, 400ms, 800ms).
    - Covers transient network issues or short hiccups.

2. **Circuit Breaker Policy**
    - Trips after 2 consecutive failures.
    - Remains OPEN for 10 seconds (fail-fast).
    - While OPEN, all calls fail immediately with **503 Service Unavailable**.
    - After 10s, transitions to HALF-OPEN → allows a single trial request.
    - If the trial succeeds, the breaker closes (normal traffic resumes).
    - If it fails, the breaker reopens for another 10s.

#### Client Behavior
- **Normal (Closed)** → Comments are filtered, persisted, and returned.
- **Failure (Breaker Open)** → Requests fail immediately with:
  ```json
  { "error": "ProfanityService unavailable. Please try again shortly." }


## Profanity Service

The `ProfanityService` filters comment text from profanity words and returns a clean profanity free comment to `CommentService`.