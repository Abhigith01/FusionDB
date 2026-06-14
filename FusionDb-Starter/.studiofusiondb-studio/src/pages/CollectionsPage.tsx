import AddIcon from "@mui/icons-material/Add";
import DatasetOutlinedIcon from "@mui/icons-material/DatasetOutlined";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    Grid,
    IconButton,
    TextField,
    Tooltip,
    Typography,
} from "@mui/material";
import { useCallback, useEffect, useState } from "react";
import {
    createCollection,
    getCollections,
    type AiCollection,
    type CreateCollectionRequest,
} from "../api/fusionDbApi";

const initialForm: CreateCollectionRequest = {
    name: "",
    description: "",
    vectorDimensions: 768,
    embeddingModel: "nomic-embed-text",
    chunkSize: 250,
    chunkOverlap: 40,
};

export default function CollectionsPage() {
    const [collections, setCollections] =
        useState<AiCollection[]>([]);

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    const [error, setError] =
        useState<string | null>(null);

    const [dialogOpen, setDialogOpen] =
        useState(false);

    const [form, setForm] =
        useState<CreateCollectionRequest>(initialForm);

    const loadCollections = useCallback(async () => {
        setLoading(true);
        setError(null);

        try {
            const result = await getCollections();
            setCollections(result);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load collections.",
            );
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadCollections();
    }, [loadCollections]);

    async function handleCreate() {
        setSaving(true);
        setError(null);

        try {
            const created = await createCollection(form);

            setCollections((current) => [
                created,
                ...current,
            ]);

            setDialogOpen(false);
            setForm(initialForm);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to create collection.",
            );
        } finally {
            setSaving(false);
        }
    }

    const formValid =
        form.name.trim().length > 0 &&
        form.embeddingModel.trim().length > 0 &&
        form.vectorDimensions > 0 &&
        form.chunkSize > 0 &&
        form.chunkOverlap >= 0 &&
        form.chunkOverlap < form.chunkSize;

    return (
        <Box>
            <Box
                sx={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    mb: 3,
                }}
            >
                <Box>
                    <Typography variant="h4">
                        Collections
                    </Typography>

                    <Typography color="text.secondary">
                        Configure document chunking and embedding models.
                    </Typography>
                </Box>

                <Box sx={{ display: "flex", gap: 1 }}>
                    <Tooltip title="Refresh">
                        <IconButton
                            onClick={() => void loadCollections()}
                            disabled={loading}
                        >
                            <RefreshIcon />
                        </IconButton>
                    </Tooltip>

                    <Button
                        variant="contained"
                        startIcon={<AddIcon />}
                        onClick={() => setDialogOpen(true)}
                    >
                        New collection
                    </Button>
                </Box>
            </Box>

            {error && (
                <Alert
                    severity="error"
                    sx={{ mb: 3 }}
                    onClose={() => setError(null)}
                >
                    {error}
                </Alert>
            )}

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

            {!loading && collections.length === 0 && (
                <Card variant="outlined">
                    <CardContent sx={{ textAlign: "center", py: 8 }}>
                        <DatasetOutlinedIcon
                            sx={{
                                fontSize: 56,
                                color: "text.secondary",
                                mb: 2,
                            }}
                        />

                        <Typography variant="h6">
                            No collections found
                        </Typography>

                        <Typography
                            color="text.secondary"
                            sx={{ mb: 2 }}
                        >
                            Create a collection to begin ingesting documents.
                        </Typography>

                        <Button
                            variant="contained"
                            startIcon={<AddIcon />}
                            onClick={() => setDialogOpen(true)}
                        >
                            Create collection
                        </Button>
                    </CardContent>
                </Card>
            )}

            {!loading && collections.length > 0 && (
                <Grid container spacing={2}>
                    {collections.map((collection) => (
                        <Grid
                            key={collection.id}
                            size={{ xs: 12, md: 6, lg: 4 }}
                        >
                            <Card
                                variant="outlined"
                                sx={{ height: "100%" }}
                            >
                                <CardContent>
                                    <Box
                                        sx={{
                                            display: "flex",
                                            justifyContent: "space-between",
                                            gap: 2,
                                        }}
                                    >
                                        <Box>
                                            <Typography variant="h6">
                                                {collection.name}
                                            </Typography>

                                            <Typography
                                                variant="body2"
                                                color="text.secondary"
                                            >
                                                {collection.description ||
                                                    "No description"}
                                            </Typography>
                                        </Box>

                                        <DatasetOutlinedIcon
                                            color="primary"
                                        />
                                    </Box>

                                    <Divider sx={{ my: 2 }} />

                                    <Box
                                        sx={{
                                            display: "flex",
                                            flexWrap: "wrap",
                                            gap: 1,
                                        }}
                                    >
                                        <Chip
                                            size="small"
                                            label={`${collection.vectorDimensions} dimensions`}
                                        />

                                        <Chip
                                            size="small"
                                            label={collection.embeddingModel}
                                        />

                                        <Chip
                                            size="small"
                                            label={`Chunk ${collection.chunkSize}`}
                                        />

                                        <Chip
                                            size="small"
                                            label={`Overlap ${collection.chunkOverlap}`}
                                        />
                                    </Box>

                                    <Typography
                                        variant="caption"
                                        color="text.secondary"
                                        sx={{
                                            display: "block",
                                            mt: 2,
                                            wordBreak: "break-all",
                                        }}
                                    >
                                        {collection.id}
                                    </Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            )}

            <Dialog
                open={dialogOpen}
                onClose={() => {
                    if (!saving) {
                        setDialogOpen(false);
                    }
                }}
                fullWidth
                maxWidth="sm"
            >
                <DialogTitle>
                    Create AI collection
                </DialogTitle>

                <DialogContent>
                    <Box
                        sx={{
                            display: "grid",
                            gap: 2,
                            pt: 1,
                        }}
                    >
                        <TextField
                            label="Name"
                            value={form.name}
                            required
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    name: event.target.value,
                                })
                            }
                        />

                        <TextField
                            label="Description"
                            value={form.description}
                            multiline
                            minRows={2}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    description: event.target.value,
                                })
                            }
                        />

                        <TextField
                            label="Embedding model"
                            value={form.embeddingModel}
                            required
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    embeddingModel: event.target.value,
                                })
                            }
                        />

                        <TextField
                            label="Vector dimensions"
                            type="number"
                            value={form.vectorDimensions}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    vectorDimensions: Number(
                                        event.target.value,
                                    ),
                                })
                            }
                        />

                        <TextField
                            label="Chunk size"
                            type="number"
                            value={form.chunkSize}
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    chunkSize: Number(event.target.value),
                                })
                            }
                        />

                        <TextField
                            label="Chunk overlap"
                            type="number"
                            value={form.chunkOverlap}
                            helperText="Must be smaller than the chunk size."
                            onChange={(event) =>
                                setForm({
                                    ...form,
                                    chunkOverlap: Number(
                                        event.target.value,
                                    ),
                                })
                            }
                        />
                    </Box>
                </DialogContent>

                <DialogActions>
                    <Button
                        onClick={() => setDialogOpen(false)}
                        disabled={saving}
                    >
                        Cancel
                    </Button>

                    <Button
                        variant="contained"
                        disabled={!formValid || saving}
                        onClick={() => void handleCreate()}
                    >
                        {saving ? "Creating…" : "Create"}
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
}