export interface ApiStatus {
    name: string;
    status: string;
    description: string;
}

export interface AiCollection {
    id: string;
    name: string;
    description: string;
    vectorDimensions: number;
    embeddingModel: string;
    chunkSize: number;
    chunkOverlap: number;
    createdAt: string;
    updatedAt: string;
}

export interface CreateCollectionRequest {
    name: string;
    description: string;
    vectorDimensions: number;
    embeddingModel: string;
    chunkSize: number;
    chunkOverlap: number;
}

export interface AiDocument {
    id: string;
    collectionId: string;
    title: string;
    content?: string;
    metadata?: Record<string, unknown>;
    contentHash?: string;
    status: string;
    failureReason?: string | null;
    createdAt: string;
    updatedAt?: string;
}

export interface UploadDocumentResponse {
    documentId: string;
    title: string;
    fileName: string;
    sizeBytes: number;
    pageCount?: number | null;
    chunkCount: number;
    status: string;
}

export interface ProcessDocumentResponse {
    documentId: string;
    chunkCount: number;
    status: string;
}

export interface SearchCollectionRequest {
    query: string;
    limit: number;
    minimumSimilarity: number;
    metadataFilter?: Record<string, unknown>;
}

export interface SearchResult {
    chunkId: string;
    documentId: string;
    documentTitle: string;
    chunkNumber: number;
    content: string;
    distance: number;
    similarity: number;
    keywordScore: number;
    semanticRank?: number | null;
    keywordRank?: number | null;
    hybridScore: number;
}

export interface SearchCollectionResponse {
    query: string;
    resultCount: number;
    results: SearchResult[];
}

export interface AskCollectionRequest {
    question: string;
    maxSources: number;
    minimumSimilarity: number;
    metadataFilter?: Record<string, unknown>;
}

export interface AskSource {
    citation: string;
    chunkId: string;
    documentId: string;
    documentTitle: string;
    chunkNumber: number;
    similarity: number;
    hybridScore: number;
    content: string;
}

export interface AskCollectionResponse {
    question: string;
    answer: string;
    grounded: boolean;
    sources: AskSource[];
}

export interface RetrievalAuditSummary {
    id: string;
    collectionId: string;
    operation: string;
    queryText: string;
    resultCount: number;
    durationMilliseconds: number;
    generationModel?: string | null;
    grounded?: boolean | null;
    status: string;
    createdAt: string;
}

export interface PagedRetrievalAuditsResponse {
    page: number;
    pageSize: number;
    totalCount: number;
    items: RetrievalAuditSummary[];
}

export interface RetrievalAuditDetail
    extends RetrievalAuditSummary {
    metadataFilter: Record<string, unknown>;
    minimumSimilarity: number;
    requestedLimit: number;
    answer?: string | null;
    results: unknown[];
    errorMessage?: string | null;
}

export interface AiDocumentChunk {
    id: string;
    documentId: string;
    chunkNumber: number;
    content: string;
    contentHash: string;
    startOffset: number;
    endOffset: number;
    createdAt: string;
}

export async function getDocumentChunks(
    collectionId: string,
    documentId: string,
): Promise<AiDocumentChunk[]> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}` +
        `/documents/${documentId}/chunks`,
    );

    return readJson<AiDocumentChunk[]>(response);
}

const apiBaseUrl =
    import.meta.env.VITE_API_BASE_URL ??
    "http://localhost:5080";

async function readJson<T>(
    response: Response,
): Promise<T> {
    if (!response.ok) {
        const responseText = await response.text();

        let message =
            responseText ||
            `Request failed with status ${response.status}`;

        try {
            const parsed = JSON.parse(responseText) as {
                message?: string;
                detail?: string;
                title?: string;
            };

            message =
                parsed.message ??
                parsed.detail ??
                parsed.title ??
                message;
        } catch {
            // Keep the original response text.
        }

        throw new Error(message);
    }

    return response.json() as Promise<T>;
}

export async function getApiStatus(): Promise<ApiStatus> {
    const response = await fetch(`${apiBaseUrl}/`);

    return readJson<ApiStatus>(response);
}

export async function getCollections(): Promise<AiCollection[]> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections`,
    );

    return readJson<AiCollection[]>(response);
}

export async function createCollection(
    request: CreateCollectionRequest,
): Promise<AiCollection> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return readJson<AiCollection>(response);
}

export async function getDocuments(
    collectionId: string,
): Promise<AiDocument[]> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}/documents`,
    );

    return readJson<AiDocument[]>(response);
}

export async function uploadDocument(
    collectionId: string,
    file: File,
): Promise<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}/documents/upload`,
        {
            method: "POST",
            body: formData,
        },
    );

    return readJson<UploadDocumentResponse>(response);
}

export async function processDocument(
    collectionId: string,
    documentId: string,
): Promise<ProcessDocumentResponse> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}/documents/${documentId}/process`,
        {
            method: "POST",
        },
    );

    return readJson<ProcessDocumentResponse>(response);
}

export async function searchCollection(
    collectionId: string,
    request: SearchCollectionRequest,
): Promise<SearchCollectionResponse> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}/search`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return readJson<SearchCollectionResponse>(response);
}

export async function askCollection(
    collectionId: string,
    request: AskCollectionRequest,
): Promise<AskCollectionResponse> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/${collectionId}/ask`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
        },
    );

    return readJson<AskCollectionResponse>(response);
}

export async function getRetrievalAudits(
    collectionId: string,
    options?: {
        operation?: string;
        status?: string;
        page?: number;
        pageSize?: number;
    },
): Promise<PagedRetrievalAuditsResponse> {
    const parameters = new URLSearchParams();

    parameters.set(
        "page",
        String(options?.page ?? 1),
    );

    parameters.set(
        "pageSize",
        String(options?.pageSize ?? 20),
    );

    if (options?.operation) {
        parameters.set(
            "operation",
            options.operation,
        );
    }

    if (options?.status) {
        parameters.set(
            "status",
            options.status,
        );
    }

    const response = await fetch(
        `${apiBaseUrl}/api/collections/` +
        `${collectionId}/retrieval-audits?` +
        parameters.toString(),
    );

    return readJson<PagedRetrievalAuditsResponse>(
        response,
    );
}

export async function getRetrievalAuditById(
    collectionId: string,
    auditId: string,
): Promise<RetrievalAuditDetail> {
    const response = await fetch(
        `${apiBaseUrl}/api/collections/` +
        `${collectionId}/retrieval-audits/${auditId}`,
    );

    return readJson<RetrievalAuditDetail>(response);
}