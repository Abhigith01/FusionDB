import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import SmartToyOutlinedIcon from "@mui/icons-material/SmartToyOutlined";
import {
    Alert,
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Divider,
    FormControl,
    InputLabel,
    MenuItem,
    Paper,
    Select,
    Slider,
    TextField,
    Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import {
    askCollection,
    getCollections,
    type AiCollection,
    type AskCollectionResponse,
} from "../api/fusionDbApi";

export default function AskFusionDbPage() {
    const [collections, setCollections] =
        useState<AiCollection[]>([]);

    const [selectedCollectionId, setSelectedCollectionId] =
        useState("");

    const [question, setQuestion] = useState("");

    const [metadataText, setMetadataText] =
        useState("");

    const [minimumSimilarity, setMinimumSimilarity] =
        useState(0.55);

    const [maxSources, setMaxSources] =
        useState(2);

    const [result, setResult] =
        useState<AskCollectionResponse | null>(null);

    const [asking, setAsking] = useState(false);

    const [elapsedSeconds, setElapsedSeconds] =
        useState<number | null>(null);

    const [error, setError] =
        useState<string | null>(null);

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

    async function handleAsk() {
        if (
            !selectedCollectionId ||
            !question.trim()
        ) {
            return;
        }

        setAsking(true);
        setError(null);
        setResult(null);
        setElapsedSeconds(null);

        const startedAt = performance.now();

        try {
            let metadataFilter:
                | Record<string, unknown>
                | undefined;

            if (metadataText.trim()) {
                const parsed = JSON.parse(metadataText);

                if (
                    typeof parsed !== "object" ||
                    parsed === null ||
                    Array.isArray(parsed)
                ) {
                    throw new Error(
                        "Metadata filter must be a JSON object.",
                    );
                }

                metadataFilter =
                    parsed as Record<string, unknown>;
            }

            const response = await askCollection(
                selectedCollectionId,
                {
                    question: question.trim(),
                    maxSources,
                    minimumSimilarity,
                    metadataFilter,
                },
            );

            setResult(response);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to generate an answer.",
            );
        } finally {
            const completedAt = performance.now();

            setElapsedSeconds(
                (completedAt - startedAt) / 1000,
            );

            setAsking(false);
        }
    }

    return (
        <Box>
            <Typography variant="h4">
                Ask FusionDb
            </Typography>

            <Typography
                color="text.secondary"
                sx={{ mb: 3 }}
            >
                Generate grounded answers from indexed documents with
                source citations.
            </Typography>

            {error && (
                <Alert
                    severity="error"
                    sx={{ mb: 2 }}
                    onClose={() => setError(null)}
                >
                    {error}
                </Alert>
            )}

            <Card variant="outlined">
                <CardContent>
                    <Box
                        sx={{
                            display: "grid",
                            gap: 2,
                        }}
                    >
                        <FormControl fullWidth>
                            <InputLabel id="ask-collection-label">
                                Collection
                            </InputLabel>

                            <Select
                                labelId="ask-collection-label"
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

                        <TextField
                            label="Question"
                            value={question}
                            multiline
                            minRows={3}
                            placeholder="Why is PostgreSQL with pgvector recommended for the MVP?"
                            onChange={(event) =>
                                setQuestion(event.target.value)
                            }
                            onKeyDown={(event) => {
                                if (
                                    event.key === "Enter" &&
                                    event.ctrlKey
                                ) {
                                    void handleAsk();
                                }
                            }}
                        />

                        <TextField
                            label="Metadata filter"
                            value={metadataText}
                            multiline
                            minRows={2}
                            placeholder={'{"fileName":"employee-handbook.pdf"}'}
                            helperText="Optional JSON filter"
                            onChange={(event) =>
                                setMetadataText(event.target.value)
                            }
                        />

                        <Box>
                            <Typography gutterBottom>
                                Minimum similarity:{" "}
                                {minimumSimilarity.toFixed(2)}
                            </Typography>

                            <Slider
                                value={minimumSimilarity}
                                min={0}
                                max={1}
                                step={0.05}
                                valueLabelDisplay="auto"
                                onChange={(_, value) =>
                                    setMinimumSimilarity(
                                        value as number,
                                    )
                                }
                            />
                        </Box>

                        <TextField
                            label="Maximum sources"
                            type="number"
                            value={maxSources}
                            slotProps={{
                                htmlInput: {
                                    min: 1,
                                    max: 10,
                                },
                            }}
                            onChange={(event) =>
                                setMaxSources(
                                    Math.max(
                                        1,
                                        Math.min(
                                            10,
                                            Number(event.target.value),
                                        ),
                                    ),
                                )
                            }
                        />

                        <Button
                            variant="contained"
                            size="large"
                            disabled={
                                asking ||
                                !selectedCollectionId ||
                                !question.trim()
                            }
                            startIcon={
                                asking ? (
                                    <CircularProgress
                                        size={18}
                                        color="inherit"
                                    />
                                ) : (
                                    <AutoAwesomeOutlinedIcon />
                                )
                            }
                            onClick={() => void handleAsk()}
                        >
                            {asking
                                ? "Generating grounded answer…"
                                : "Ask FusionDb"}
                        </Button>

                        {asking && (
                            <Alert severity="info">
                                The local language model may take around
                                20–50 seconds to respond.
                            </Alert>
                        )}
                    </Box>
                </CardContent>
            </Card>

            {elapsedSeconds !== null && !asking && (
                <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 2 }}
                >
                    Completed in {elapsedSeconds.toFixed(1)} seconds
                </Typography>
            )}

            {result && (
                <Box
                    sx={{
                        display: "grid",
                        gap: 2,
                        mt: 3,
                    }}
                >
                    <Paper
                        variant="outlined"
                        sx={{ p: 3 }}
                    >
                        <Box
                            sx={{
                                display: "flex",
                                alignItems: "center",
                                gap: 1,
                                mb: 2,
                            }}
                        >
                            <SmartToyOutlinedIcon color="primary" />

                            <Typography variant="h6">
                                Grounded answer
                            </Typography>

                            <Chip
                                size="small"
                                color={
                                    result.grounded
                                        ? "success"
                                        : "warning"
                                }
                                label={
                                    result.grounded
                                        ? "Grounded"
                                        : "No supported answer"
                                }
                            />
                        </Box>

                        <Divider sx={{ mb: 2 }} />

                        <Typography
                            sx={{
                                whiteSpace: "pre-wrap",
                                lineHeight: 1.7,
                            }}
                        >
                            {result.answer}
                        </Typography>
                    </Paper>

                    {result.sources.length > 0 && (
                        <Box>
                            <Typography
                                variant="h6"
                                sx={{ mb: 1.5 }}
                            >
                                Sources
                            </Typography>

                            <Box
                                sx={{
                                    display: "grid",
                                    gap: 2,
                                }}
                            >
                                {result.sources.map((source) => (
                                    <Paper
                                        key={source.chunkId}
                                        variant="outlined"
                                        sx={{ p: 2 }}
                                    >
                                        <Box
                                            sx={{
                                                display: "flex",
                                                justifyContent:
                                                    "space-between",
                                                gap: 2,
                                                mb: 1,
                                            }}
                                        >
                                            <Box>
                                                <Typography fontWeight={600}>
                                                    [{source.citation}]{" "}
                                                    {source.documentTitle}
                                                </Typography>

                                                <Typography
                                                    variant="body2"
                                                    color="text.secondary"
                                                >
                                                    Chunk {source.chunkNumber}
                                                </Typography>
                                            </Box>

                                            <Box
                                                sx={{
                                                    display: "flex",
                                                    flexWrap: "wrap",
                                                    gap: 1,
                                                }}
                                            >
                                                <Chip
                                                    size="small"
                                                    label={`Similarity ${source.similarity.toFixed(
                                                        3,
                                                    )}`}
                                                />

                                                <Chip
                                                    size="small"
                                                    color="primary"
                                                    label={`Hybrid ${source.hybridScore.toFixed(
                                                        3,
                                                    )}`}
                                                />
                                            </Box>
                                        </Box>

                                        <Divider sx={{ my: 1.5 }} />

                                        <Typography
                                            variant="body2"
                                            sx={{
                                                whiteSpace: "pre-wrap",
                                            }}
                                        >
                                            {source.content}
                                        </Typography>
                                    </Paper>
                                ))}
                            </Box>
                        </Box>
                    )}
                </Box>
            )}
        </Box>
    );
}