import CloudUploadOutlinedIcon from "@mui/icons-material/CloudUploadOutlined";
import DescriptionOutlinedIcon from "@mui/icons-material/DescriptionOutlined";
import RefreshIcon from "@mui/icons-material/Refresh";
import ReplayIcon from "@mui/icons-material/Replay";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    FormControl,
    IconButton,
    InputLabel,
    MenuItem,
    Select,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Tooltip,
    Typography,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
} from "@mui/material";
import { useCallback, useEffect, useRef, useState } from "react";
import {
    getCollections,
    getDocuments,
    processDocument,
    uploadDocument,
    getDocumentChunks,
    type AiDocumentChunk,
    type AiCollection,
    type AiDocument,
} from "../api/fusionDbApi";

type ChipColor =
    | "default"
    | "success"
    | "warning"
    | "error"
    | "info";

function getStatusColor(status: string): ChipColor {
    switch (status.toLowerCase()) {
        case "ready":
            return "success";

        case "processing":
            return "info";

        case "pending":
            return "warning";

        case "failed":
            return "error";

        default:
            return "default";
    }
}

function formatDate(value: string): string {
    return new Date(value).toLocaleString();
}

export default function DocumentsPage() {
    const [selectedDocument, setSelectedDocument] =
        useState<AiDocument | null>(null);

    const [chunks, setChunks] =
        useState<AiDocumentChunk[]>([]);

    const [loadingChunks, setLoadingChunks] =
        useState(false);
    const fileInputRef =
        useRef<HTMLInputElement | null>(null);

    const [collections, setCollections] =
        useState<AiCollection[]>([]);

    const [selectedCollectionId, setSelectedCollectionId] =
        useState("");

    const [documents, setDocuments] =
        useState<AiDocument[]>([]);

    const [loadingCollections, setLoadingCollections] =
        useState(true);

    const [loadingDocuments, setLoadingDocuments] =
        useState(false);

    const [uploading, setUploading] =
        useState(false);

    const [processingId, setProcessingId] =
        useState<string | null>(null);

    const [error, setError] =
        useState<string | null>(null);

    const [message, setMessage] =
        useState<string | null>(null);

    const loadDocuments = useCallback(
        async (collectionId: string) => {
            if (!collectionId) {
                setDocuments([]);
                return;
            }

            setLoadingDocuments(true);
            setError(null);

            try {
                const result =
                    await getDocuments(collectionId);

                setDocuments(result);
            } catch (requestError) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to load documents.",
                );
            } finally {
                setLoadingDocuments(false);
            }
        },
        [],
    );

    useEffect(() => {
        async function loadCollections() {
            setLoadingCollections(true);

            try {
                const result = await getCollections();

                setCollections(result);

                if (result.length > 0) {
                    setSelectedCollectionId(result[0].id);
                }
            } catch (requestError) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to load collections.",
                );
            } finally {
                setLoadingCollections(false);
            }
        }

        void loadCollections();
    }, []);

    useEffect(() => {
        if (selectedCollectionId) {
            void loadDocuments(selectedCollectionId);
        }
    }, [selectedCollectionId, loadDocuments]);

    async function handleFileSelected(
        event: React.ChangeEvent<HTMLInputElement>,
    ) {
        const file = event.target.files?.[0];

        event.target.value = "";

        if (!file || !selectedCollectionId) {
            return;
        }

        setUploading(true);
        setError(null);
        setMessage(null);

        try {
            const result =
                await uploadDocument(
                    selectedCollectionId,
                    file,
                );

            setMessage(
                `${result.fileName} uploaded successfully. ` +
                `${result.chunkCount} chunk(s) created.`,
            );

            await loadDocuments(selectedCollectionId);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to upload the document.",
            );
        } finally {
            setUploading(false);
        }
    }

    async function handleProcess(documentId: string) {
        if (!selectedCollectionId) {
            return;
        }

        setProcessingId(documentId);
        setError(null);
        setMessage(null);

        try {
            const result =
                await processDocument(
                    selectedCollectionId,
                    documentId,
                );

            setMessage(
                `Document processed successfully. ` +
                `${result.chunkCount} chunk(s) created.`,
            );

            await loadDocuments(selectedCollectionId);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to process the document.",
            );
        } finally {
            setProcessingId(null);
        }
    }

    async function handleViewChunks(document: AiDocument) {
        if (!selectedCollectionId) {
            return;
        }

        setSelectedDocument(document);
        setChunks([]);
        setLoadingChunks(true);
        setError(null);

        try {
            const response = await getDocumentChunks(
                selectedCollectionId,
                document.id,
            );

            setChunks(response);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load document chunks.",
            );

            setSelectedDocument(null);
        } finally {
            setLoadingChunks(false);
        }
    }

    return (
        <Box>
            <Box
                sx={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    gap: 2,
                    mb: 3,
                }}
            >
                <Box>
                    <Typography variant="h4">
                        Documents
                    </Typography>

                    <Typography color="text.secondary">
                        Upload, process and inspect collection documents.
                    </Typography>
                </Box>

                <Box
                    sx={{
                        display: "flex",
                        alignItems: "center",
                        gap: 1,
                    }}
                >
                    <Tooltip title="Refresh">
                        <span>
                            <IconButton
                                disabled={
                                    !selectedCollectionId ||
                                    loadingDocuments
                                }
                                onClick={() =>
                                    void loadDocuments(
                                        selectedCollectionId,
                                    )
                                }
                            >
                                <RefreshIcon />
                            </IconButton>
                        </span>
                    </Tooltip>

                    <input
                        ref={fileInputRef}
                        type="file"
                        hidden
                        accept=".txt,.md,.markdown,.pdf"
                        onChange={(event) =>
                            void handleFileSelected(event)
                        }
                    />

                    <Button
                        variant="contained"
                        startIcon={
                            uploading ? (
                                <CircularProgress
                                    size={18}
                                    color="inherit"
                                />
                            ) : (
                                <CloudUploadOutlinedIcon />
                            )
                        }
                        disabled={
                            uploading ||
                            !selectedCollectionId
                        }
                        onClick={() =>
                            fileInputRef.current?.click()
                        }
                    >
                        {uploading
                            ? "Uploading…"
                            : "Upload document"}
                    </Button>
                </Box>
            </Box>

            {error && (
                <Alert
                    severity="error"
                    sx={{ mb: 2 }}
                    onClose={() => setError(null)}
                >
                    {error}
                </Alert>
            )}

            {message && (
                <Alert
                    severity="success"
                    sx={{ mb: 2 }}
                    onClose={() => setMessage(null)}
                >
                    {message}
                </Alert>
            )}

            <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                    <FormControl
                        fullWidth
                        disabled={loadingCollections}
                    >
                        <InputLabel id="collection-select-label">
                            Collection
                        </InputLabel>

                        <Select
                            labelId="collection-select-label"
                            label="Collection"
                            value={selectedCollectionId}
                            onChange={(event) =>
                                setSelectedCollectionId(
                                    event.target.value,
                                )
                            }
                        >
                            {collections.map((collection) => (
                                <MenuItem
                                    key={collection.id}
                                    value={collection.id}
                                >
                                    {collection.name}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                </CardContent>
            </Card>

            {loadingDocuments && (
                <Box
                    sx={{
                        display: "flex",
                        justifyContent: "center",
                        py: 8,
                    }}
                >
                    <CircularProgress />
                </Box>
            )}

            {!loadingDocuments &&
                selectedCollectionId &&
                documents.length === 0 && (
                    <Card variant="outlined">
                        <CardContent
                            sx={{
                                textAlign: "center",
                                py: 8,
                            }}
                        >
                            <DescriptionOutlinedIcon
                                sx={{
                                    fontSize: 56,
                                    color: "text.secondary",
                                    mb: 2,
                                }}
                            />

                            <Typography variant="h6">
                                No documents found
                            </Typography>

                            <Typography
                                color="text.secondary"
                                sx={{ mb: 2 }}
                            >
                                Upload a TXT, Markdown or PDF document.
                            </Typography>

                            <Button
                                variant="contained"
                                startIcon={
                                    <CloudUploadOutlinedIcon />
                                }
                                onClick={() =>
                                    fileInputRef.current?.click()
                                }
                            >
                                Upload document
                            </Button>
                        </CardContent>
                    </Card>
                )}

            {!loadingDocuments &&
                documents.length > 0 && (
                    <TableContainer
                        component={Card}
                        variant="outlined"
                    >
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Title</TableCell>
                                    <TableCell>Status</TableCell>
                                    <TableCell>Created</TableCell>
                                    <TableCell>Document ID</TableCell>
                                    <TableCell align="right">
                                        Actions
                                    </TableCell>
                                </TableRow>
                            </TableHead>

                            <TableBody>
                                {documents.map((document) => (
                                    <TableRow
                                        key={document.id}
                                        hover
                                    >
                                        <TableCell>
                                            <Typography sx={{ fontWeight: 600 }}>
                                                {document.title}
                                            </Typography>

                                            {document.failureReason && (
                                                <Typography
                                                    variant="caption"
                                                    color="error"
                                                >
                                                    {document.failureReason}
                                                </Typography>
                                            )}
                                        </TableCell>

                                        <TableCell>
                                            <Chip
                                                size="small"
                                                label={document.status}
                                                color={getStatusColor(
                                                    document.status,
                                                )}
                                            />
                                        </TableCell>

                                        <TableCell>
                                            {formatDate(
                                                document.createdAt,
                                            )}
                                        </TableCell>

                                        <TableCell>
                                            <Typography
                                                variant="caption"
                                                sx={{
                                                    wordBreak: "break-all",
                                                }}
                                            >
                                                {document.id}
                                            </Typography>
                                        </TableCell>

                                        <TableCell align="right">
                                            <Tooltip title="View chunks">
                                                <IconButton
                                                    onClick={() =>
                                                        void handleViewChunks(document)
                                                    }
                                                >
                                                    <VisibilityOutlinedIcon />
                                                </IconButton>
                                            </Tooltip>

                                            <Tooltip title="Reprocess">
                                                <span>
                                                    <IconButton
                                                        disabled={processingId === document.id}
                                                        onClick={() =>
                                                            void handleProcess(document.id)
                                                        }
                                                    >
                                                        {processingId === document.id ? (
                                                            <CircularProgress size={20} />
                                                        ) : (
                                                            <ReplayIcon />
                                                        )}
                                                    </IconButton>
                                                </span>
                                            </Tooltip>
                                        </TableCell>

                                        <TableCell align="right">
                                            <Tooltip title="Reprocess">
                                                <span>
                                                    <IconButton
                                                        disabled={
                                                            processingId ===
                                                            document.id
                                                        }
                                                        onClick={() =>
                                                            void handleProcess(
                                                                document.id,
                                                            )
                                                        }
                                                    >
                                                        {processingId ===
                                                            document.id ? (
                                                            <CircularProgress
                                                                size={20}
                                                            />
                                                        ) : (
                                                            <ReplayIcon />
                                                        )}
                                                    </IconButton>
                                                </span>
                                            </Tooltip>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                )}
            <Dialog
                open={selectedDocument !== null}
                onClose={() => {
                    if (!loadingChunks) {
                        setSelectedDocument(null);
                        setChunks([]);
                    }
                }}
                fullWidth
                maxWidth="lg"
            >
                <DialogTitle>
                    {selectedDocument?.title} — Chunks
                </DialogTitle>

                <DialogContent dividers>
                    {loadingChunks && (
                        <Box
                            sx={{
                                display: "flex",
                                justifyContent: "center",
                                py: 8,
                            }}
                        >
                            <CircularProgress />
                        </Box>
                    )}

                    {!loadingChunks && chunks.length === 0 && (
                        <Alert severity="info">
                            This document has no chunks. Try reprocessing it.
                        </Alert>
                    )}

                    {!loadingChunks && chunks.length > 0 && (
                        <Box
                            sx={{
                                display: "grid",
                                gap: 2,
                            }}
                        >
                            <Typography color="text.secondary">
                                {chunks.length} chunk(s)
                            </Typography>

                            {chunks.map((chunk) => (
                                <Card
                                    key={chunk.id}
                                    variant="outlined"
                                >
                                    <CardContent>
                                        <Box
                                            sx={{
                                                display: "flex",
                                                justifyContent: "space-between",
                                                alignItems: "center",
                                                gap: 2,
                                            }}
                                        >
                                            <Typography variant="h6">
                                                Chunk {chunk.chunkNumber}
                                            </Typography>

                                            <Chip
                                                size="small"
                                                label={
                                                    `${chunk.startOffset}–` +
                                                    `${chunk.endOffset}`
                                                }
                                            />
                                        </Box>

                                        <Divider sx={{ my: 1.5 }} />

                                        <Typography
                                            variant="body2"
                                            sx={{
                                                whiteSpace: "pre-wrap",
                                                lineHeight: 1.7,
                                            }}
                                        >
                                            {chunk.content}
                                        </Typography>

                                        <Typography
                                            variant="caption"
                                            color="text.secondary"
                                            sx={{
                                                display: "block",
                                                mt: 2,
                                                wordBreak: "break-all",
                                            }}
                                        >
                                            SHA-256: {chunk.contentHash}
                                        </Typography>
                                    </CardContent>
                                </Card>
                            ))}
                        </Box>
                    )}
                </DialogContent>

                <DialogActions>
                    <Button
                        onClick={() => {
                            setSelectedDocument(null);
                            setChunks([]);
                        }}
                        disabled={loadingChunks}
                    >
                        Close
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}