import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
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
    getCollections,
    searchCollection,
    type AiCollection,
    type SearchResult,
} from "../api/fusionDbApi";

export default function HybridSearchPage() {
    const [collections, setCollections] =
        useState<AiCollection[]>([]);

    const [selectedCollectionId, setSelectedCollectionId] =
        useState("");

    const [query, setQuery] = useState("");
    const [limit, setLimit] = useState(5);

    const [minimumSimilarity, setMinimumSimilarity] =
        useState(0.55);

    const [metadataText, setMetadataText] =
        useState("");

    const [results, setResults] =
        useState<SearchResult[]>([]);

    const [searching, setSearching] =
        useState(false);

    const [error, setError] =
        useState<string | null>(null);

    useEffect(() => {
        async function loadCollections() {
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
            }
        }

        void loadCollections();
    }, []);

    async function handleSearch() {
        if (!selectedCollectionId || !query.trim()) {
            return;
        }

        setSearching(true);
        setError(null);
        setResults([]);

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

            const response = await searchCollection(
                selectedCollectionId,
                {
                    query: query.trim(),
                    limit,
                    minimumSimilarity,
                    metadataFilter,
                },
            );

            setResults(response.results);
        } catch (requestError) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Search failed.",
            );
        } finally {
            setSearching(false);
        }
    }

    return (
        <Box>
            <Typography variant="h4">
                Hybrid Search
            </Typography>

            <Typography
                color="text.secondary"
                sx={{ mb: 3 }}
            >
                Combine semantic vector search, full-text search and
                metadata filtering.
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
                            <InputLabel id="search-collection-label">
                                Collection
                            </InputLabel>

                            <Select
                                labelId="search-collection-label"
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
                            label="Search query"
                            value={query}
                            multiline
                            minRows={2}
                            placeholder="Why is PostgreSQL with pgvector recommended?"
                            onChange={(event) =>
                                setQuery(event.target.value)
                            }
                            onKeyDown={(event) => {
                                if (
                                    event.key === "Enter" &&
                                    event.ctrlKey
                                ) {
                                    void handleSearch();
                                }
                            }}
                        />

                        <TextField
                            label="Metadata filter"
                            value={metadataText}
                            multiline
                            minRows={2}
                            placeholder={'{"fileName":"employee-handbook.pdf"}'}
                            helperText="Optional JSON object"
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
                            label="Result limit"
                            type="number"
                            value={limit}
                            slotProps={{
                                htmlInput: {
                                    min: 1,
                                    max: 50,
                                },
                            }}
                            onChange={(event) =>
                                setLimit(
                                    Math.max(
                                        1,
                                        Math.min(
                                            50,
                                            Number(event.target.value),
                                        ),
                                    ),
                                )
                            }
                        />

                        <Button
                            variant="contained"
                            size="large"
                            startIcon={
                                searching ? (
                                    <CircularProgress
                                        size={18}
                                        color="inherit"
                                    />
                                ) : (
                                    <SearchOutlinedIcon />
                                )
                            }
                            disabled={
                                searching ||
                                !selectedCollectionId ||
                                !query.trim()
                            }
                            onClick={() => void handleSearch()}
                        >
                            {searching
                                ? "Searching…"
                                : "Search"}
                        </Button>
                    </Box>
                </CardContent>
            </Card>

            {!searching && results.length === 0 && query && (
                <Alert severity="info" sx={{ mt: 3 }}>
                    No matching chunks were found.
                </Alert>
            )}

            <Box
                sx={{
                    display: "grid",
                    gap: 2,
                    mt: 3,
                }}
            >
                {results.map((result, index) => (
                    <Paper
                        key={result.chunkId}
                        variant="outlined"
                        sx={{ p: 2 }}
                    >
                        <Box
                            sx={{
                                display: "flex",
                                justifyContent: "space-between",
                                gap: 2,
                                mb: 1,
                            }}
                        >
                            <Box>
                                <Typography variant="h6">
                                    {index + 1}. {result.documentTitle}
                                </Typography>

                                <Typography
                                    variant="body2"
                                    color="text.secondary"
                                >
                                    Chunk {result.chunkNumber}
                                </Typography>
                            </Box>

                            <Chip
                                color="primary"
                                label={`Hybrid ${result.hybridScore.toFixed(
                                    3,
                                )}`}
                            />
                        </Box>

                        <Divider sx={{ my: 1.5 }} />

                        <Typography
                            variant="body2"
                            sx={{
                                whiteSpace: "pre-wrap",
                                mb: 2,
                            }}
                        >
                            {result.content}
                        </Typography>

                        <Box
                            sx={{
                                display: "flex",
                                flexWrap: "wrap",
                                gap: 1,
                            }}
                        >
                            <Chip
                                size="small"
                                label={`Similarity ${result.similarity.toFixed(
                                    3,
                                )}`}
                            />

                            <Chip
                                size="small"
                                label={`Keyword ${result.keywordScore.toFixed(
                                    3,
                                )}`}
                            />

                            <Chip
                                size="small"
                                label={`Semantic rank ${result.semanticRank ?? "-"
                                    }`}
                            />

                            <Chip
                                size="small"
                                label={`Keyword rank ${result.keywordRank ?? "-"
                                    }`}
                            />
                        </Box>
                    </Paper>
                ))}
            </Box>
        </Box>
    );
}