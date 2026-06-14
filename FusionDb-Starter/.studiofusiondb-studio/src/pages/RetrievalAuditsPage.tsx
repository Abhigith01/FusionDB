import RefreshIcon from "@mui/icons-material/Refresh";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import {
    Alert,
    Box,
    Button,
    Card,
    Chip,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
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
} from "@mui/material";
import { useCallback, useEffect, useState } from "react";
import {
    getCollections,
    getRetrievalAuditById,
    getRetrievalAudits,
    type AiCollection,
    type RetrievalAuditDetail,
    type RetrievalAuditSummary,
} from "../api/fusionDbApi";

function formatDuration(milliseconds: number): string {
    if (milliseconds < 1000) {
        return `${milliseconds} ms`;
    }

    return `${(milliseconds / 1000).toFixed(1)} s`;
}

function formatDate(value: string): string {
    return new Date(value).toLocaleString();
}

function getStatusColor(
    status: string,
): "default" | "success" | "warning" | "error" {
    switch (status.toLowerCase()) {
        case "succeeded":
            return "success";

        case "noresults":
        case "noanswer":
            return "warning";

        case "failed":
            return "error";

        default:
            return "default";
    }
}

export default function RetrievalAuditsPage() {
    const [collections, setCollections] =
        useState<AiCollection[]>([]);

    const [selectedCollectionId, setSelectedCollectionId] =
        useState("");

    const [operation, setOperation] = useState("");
    const [status, setStatus] = useState("");

    const [audits, setAudits] =
        useState<RetrievalAuditSummary[]>([]);

    const [totalCount, setTotalCount] = useState(0);

    const [loading, setLoading] = useState(false);
    const [detailLoading, setDetailLoading] =
        useState(false);

    const [selectedAudit, setSelectedAudit] =
        useState<RetrievalAuditDetail | null>(null);

    const [error, setError] =
        useState<string | null>(null);

    const loadAudits = useCallback(async () => {
        if (!selectedCollectionId) {
            setAudits([]);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const response = await getRetrievalAudits(
                selectedCollectionId,
                {
                    operation: operation || undefined,
                    status: status || undefined,
                    page: 1,
                    pageSize: 50,
                },
            );

            setAudits(response.items);
            setTotalCount(response.totalCount);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load retrieval audits.",
            );
        } finally {
            setLoading(false);
        }
    }, [selectedCollectionId, operation, status]);

    useEffect(() => {
        async function loadCollections() {
            try {
                const response = await getCollections();

                setCollections(response);

                if (response.length > 0) {
                    setSelectedCollectionId(response[0].id);
                }
            } catch (requestError) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to load collections.",
                );
            }
        }

        void loadCollections();
    }, []);

    useEffect(() => {
        void loadAudits();
    }, [loadAudits]);

    async function openAudit(auditId: string) {
        if (!selectedCollectionId) {
            return;
        }

        setDetailLoading(true);
        setError(null);

        try {
            const response =
                await getRetrievalAuditById(
                    selectedCollectionId,
                    auditId,
                );

            setSelectedAudit(response);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load audit details.",
            );
        } finally {
            setDetailLoading(false);
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
                        Retrieval Audits
                    </Typography>

                    <Typography color="text.secondary">
                        Inspect queries, scores, models, answers and execution
                        duration.
                    </Typography>
                </Box>

                <Tooltip title="Refresh">
                    <IconButton
                        disabled={loading}
                        onClick={() => void loadAudits()}
                    >
                        <RefreshIcon />
                    </IconButton>
                </Tooltip>
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

            <Card variant="outlined" sx={{ mb: 3, p: 2 }}>
                <Box
                    sx={{
                        display: "grid",
                        gridTemplateColumns: {
                            xs: "1fr",
                            md: "2fr 1fr 1fr",
                        },
                        gap: 2,
                    }}
                >
                    <FormControl fullWidth>
                        <InputLabel id="audit-collection-label">
                            Collection
                        </InputLabel>

                        <Select
                            labelId="audit-collection-label"
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

                    <FormControl fullWidth>
                        <InputLabel id="operation-label">
                            Operation
                        </InputLabel>

                        <Select
                            labelId="operation-label"
                            label="Operation"
                            value={operation}
                            onChange={(event) =>
                                setOperation(event.target.value)
                            }
                        >
                            <MenuItem value="">
                                All operations
                            </MenuItem>

                            <MenuItem value="search">
                                Search
                            </MenuItem>

                            <MenuItem value="ask">
                                Ask
                            </MenuItem>
                        </Select>
                    </FormControl>

                    <FormControl fullWidth>
                        <InputLabel id="status-label">
                            Status
                        </InputLabel>

                        <Select
                            labelId="status-label"
                            label="Status"
                            value={status}
                            onChange={(event) =>
                                setStatus(event.target.value)
                            }
                        >
                            <MenuItem value="">
                                All statuses
                            </MenuItem>

                            <MenuItem value="Succeeded">
                                Succeeded
                            </MenuItem>

                            <MenuItem value="NoResults">
                                No results
                            </MenuItem>

                            <MenuItem value="NoAnswer">
                                No answer
                            </MenuItem>
                        </Select>
                    </FormControl>
                </Box>
            </Card>

            <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mb: 1 }}
            >
                {totalCount} audit record(s)
            </Typography>

            {loading && (
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

            {!loading && (
                <TableContainer
                    component={Card}
                    variant="outlined"
                >
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>Operation</TableCell>
                                <TableCell>Query</TableCell>
                                <TableCell>Results</TableCell>
                                <TableCell>Duration</TableCell>
                                <TableCell>Grounded</TableCell>
                                <TableCell>Status</TableCell>
                                <TableCell>Created</TableCell>
                                <TableCell align="right">
                                    Details
                                </TableCell>
                            </TableRow>
                        </TableHead>

                        <TableBody>
                            {audits.map((audit) => (
                                <TableRow
                                    key={audit.id}
                                    hover
                                >
                                    <TableCell>
                                        <Chip
                                            size="small"
                                            label={audit.operation}
                                            color={
                                                audit.operation === "ask"
                                                    ? "primary"
                                                    : "default"
                                            }
                                        />
                                    </TableCell>

                                    <TableCell>
                                        <Typography
                                            variant="body2"
                                            sx={{
                                                maxWidth: 360,
                                                overflow: "hidden",
                                                textOverflow: "ellipsis",
                                                whiteSpace: "nowrap",
                                            }}
                                        >
                                            {audit.queryText}
                                        </Typography>
                                    </TableCell>

                                    <TableCell>
                                        {audit.resultCount}
                                    </TableCell>

                                    <TableCell>
                                        {formatDuration(
                                            audit.durationMilliseconds,
                                        )}
                                    </TableCell>

                                    <TableCell>
                                        {audit.grounded === null ||
                                            audit.grounded === undefined
                                            ? "-"
                                            : audit.grounded
                                                ? "Yes"
                                                : "No"}
                                    </TableCell>

                                    <TableCell>
                                        <Chip
                                            size="small"
                                            label={audit.status}
                                            color={getStatusColor(
                                                audit.status,
                                            )}
                                        />
                                    </TableCell>

                                    <TableCell>
                                        {formatDate(audit.createdAt)}
                                    </TableCell>

                                    <TableCell align="right">
                                        <Tooltip title="View details">
                                            <IconButton
                                                onClick={() =>
                                                    void openAudit(audit.id)
                                                }
                                            >
                                                <VisibilityOutlinedIcon />
                                            </IconButton>
                                        </Tooltip>
                                    </TableCell>
                                </TableRow>
                            ))}

                            {audits.length === 0 && (
                                <TableRow>
                                    <TableCell
                                        colSpan={8}
                                        align="center"
                                        sx={{ py: 6 }}
                                    >
                                        No audit records found.
                                    </TableCell>
                                </TableRow>
                            )}
                        </TableBody>
                    </Table>
                </TableContainer>
            )}

            <Dialog
                open={
                    selectedAudit !== null ||
                    detailLoading
                }
                onClose={() => {
                    if (!detailLoading) {
                        setSelectedAudit(null);
                    }
                }}
                fullWidth
                maxWidth="lg"
            >
                <DialogTitle>
                    Retrieval audit details
                </DialogTitle>

                <DialogContent dividers>
                    {detailLoading && (
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

                    {selectedAudit && !detailLoading && (
                        <Box
                            sx={{
                                display: "grid",
                                gap: 3,
                            }}
                        >
                            <Box
                                sx={{
                                    display: "flex",
                                    flexWrap: "wrap",
                                    gap: 1,
                                }}
                            >
                                <Chip
                                    label={selectedAudit.operation}
                                    color="primary"
                                />

                                <Chip
                                    label={selectedAudit.status}
                                    color={getStatusColor(
                                        selectedAudit.status,
                                    )}
                                />

                                <Chip
                                    label={formatDuration(
                                        selectedAudit.durationMilliseconds,
                                    )}
                                />

                                {selectedAudit.generationModel && (
                                    <Chip
                                        label={
                                            selectedAudit.generationModel
                                        }
                                    />
                                )}
                            </Box>

                            <Box>
                                <Typography
                                    variant="subtitle2"
                                    color="text.secondary"
                                >
                                    Query
                                </Typography>

                                <Typography>
                                    {selectedAudit.queryText}
                                </Typography>
                            </Box>

                            {selectedAudit.answer && (
                                <Box>
                                    <Typography
                                        variant="subtitle2"
                                        color="text.secondary"
                                    >
                                        Answer
                                    </Typography>

                                    <Typography
                                        sx={{
                                            whiteSpace: "pre-wrap",
                                            lineHeight: 1.7,
                                        }}
                                    >
                                        {selectedAudit.answer}
                                    </Typography>
                                </Box>
                            )}

                            <Box>
                                <Typography
                                    variant="subtitle2"
                                    color="text.secondary"
                                >
                                    Metadata filter
                                </Typography>

                                <Box
                                    component="pre"
                                    sx={{
                                        p: 2,
                                        bgcolor: "grey.100",
                                        borderRadius: 1,
                                        overflow: "auto",
                                    }}
                                >
                                    {JSON.stringify(
                                        selectedAudit.metadataFilter,
                                        null,
                                        2,
                                    )}
                                </Box>
                            </Box>

                            <Box>
                                <Typography
                                    variant="subtitle2"
                                    color="text.secondary"
                                >
                                    Retrieved results
                                </Typography>

                                <Box
                                    component="pre"
                                    sx={{
                                        p: 2,
                                        maxHeight: 420,
                                        bgcolor: "grey.100",
                                        borderRadius: 1,
                                        overflow: "auto",
                                        whiteSpace: "pre-wrap",
                                        wordBreak: "break-word",
                                    }}
                                >
                                    {JSON.stringify(
                                        selectedAudit.results,
                                        null,
                                        2,
                                    )}
                                </Box>
                            </Box>

                            {selectedAudit.errorMessage && (
                                <Alert severity="error">
                                    {selectedAudit.errorMessage}
                                </Alert>
                            )}
                        </Box>
                    )}
                </DialogContent>

                <DialogActions>
                    <Button
                        onClick={() =>
                            setSelectedAudit(null)
                        }
                        disabled={detailLoading}
                    >
                        Close
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}