import {
  CheckCircleOutlined,
  CloudOff,
  DescriptionOutlined,
  FolderOutlined,
  HistoryOutlined,
  SearchOutlined,
  SmartToyOutlined,
} from "@mui/icons-material";
import {
  Alert,
  AppBar,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  CssBaseline,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import {
  getApiStatus,
  type ApiStatus,
} from "./api/fusionDbApi";
import CollectionsPage from "./pages/CollectionsPage";
import DocumentsPage from "./pages/DocumentsPage";
import HybridSearchPage from "./pages/HybridSearchPage";
import AskFusionDbPage from "./pages/AskFusionDbPage";
import RetrievalAuditsPage from "./pages/RetrievalAuditsPage";

const navigationItems = [
  {
    label: "Collections",
    icon: <FolderOutlined />,
  },
  {
    label: "Documents",
    icon: <DescriptionOutlined />,
  },
  {
    label: "Hybrid Search",
    icon: <SearchOutlined />,
  },
  {
    label: "Ask FusionDb",
    icon: <SmartToyOutlined />,
  },
  {
    label: "Retrieval Audits",
    icon: <HistoryOutlined />,
  },
];

function App() {
  const [apiStatus, setApiStatus] =
    useState<ApiStatus | null>(null);

  const [loading, setLoading] = useState(true);

  const [error, setError] =
    useState<string | null>(null);

  const [selectedPage, setSelectedPage] =
    useState("Dashboard");

  useEffect(() => {
    async function loadStatus() {
      try {
        const status = await getApiStatus();
        setApiStatus(status);
      } catch (requestError) {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Unable to connect to FusionDb.",
        );
      } finally {
        setLoading(false);
      }
    }

    void loadStatus();
  }, []);

  function renderPage() {
    switch (selectedPage) {
      case "Collections":
        return <CollectionsPage />;

      case "Documents":
        return <DocumentsPage />;

      case "Hybrid Search":
        return <HybridSearchPage />;

      case "Ask FusionDb":
        return <AskFusionDbPage />;

      case "Retrieval Audits":
        return <RetrievalAuditsPage />;

      default:
        return (
          <Stack spacing={3}>
            <Box>
              <Typography variant="h4">
                Dashboard
              </Typography>

              <Typography color="text.secondary">
                Manage operational and AI data from one place.
              </Typography>
            </Box>

            <Card>
              <CardContent>
                <Stack spacing={2}>
                  <Typography variant="h6">
                    API connection
                  </Typography>

                  {loading && (
                    <Stack
                      direction="row"
                      spacing={2}
                      sx={{ alignItems: "center" }}
                    >
                      <CircularProgress size={22} />

                      <Typography>
                        Connecting to FusionDb…
                      </Typography>
                    </Stack>
                  )}

                  {error && (
                    <Alert
                      severity="error"
                      icon={<CloudOff />}
                    >
                      {error}
                    </Alert>
                  )}

                  {apiStatus && (
                    <Alert
                      severity="success"
                      icon={<CheckCircleOutlined />}
                    >
                      <Typography sx={{ fontWeight: 600 }}>
                        {apiStatus.name} is {apiStatus.status}
                      </Typography>

                      <Typography variant="body2">
                        {apiStatus.description}
                      </Typography>
                    </Alert>
                  )}
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        );
    }
  }

  return (
    <>
      <CssBaseline />

      <AppBar position="fixed">
        <Toolbar>
          <Box
            onClick={() => setSelectedPage("Dashboard")}
            sx={{
              display: "flex",
              alignItems: "center",
              flexGrow: 1,
              cursor: "pointer",
            }}
          >
            <SmartToyOutlined sx={{ mr: 1.5 }} />

            <Typography
              variant="h6"
              component="h1"
            >
              FusionDb Studio
            </Typography>
          </Box>

          <Chip
            label="Local Development"
            size="small"
            variant="outlined"
            sx={{
              color: "white",
              borderColor: "rgba(255,255,255,0.5)",
            }}
          />
        </Toolbar>
      </AppBar>

      <Box sx={{ display: "flex", pt: 8 }}>
        <Box
          component="nav"
          sx={{
            width: 250,
            minWidth: 250,
            minHeight: "calc(100vh - 64px)",
            borderRight: 1,
            borderColor: "divider",
            bgcolor: "background.paper",
          }}
        >
          <List>
            {navigationItems.map((item) => (
              <ListItemButton
                key={item.label}
                selected={selectedPage === item.label}
                onClick={() =>
                  setSelectedPage(item.label)
                }
              >
                <ListItemIcon>
                  {item.icon}
                </ListItemIcon>

                <ListItemText
                  primary={item.label}
                />
              </ListItemButton>
            ))}
          </List>
        </Box>

        <Container
          component="main"
          maxWidth="lg"
          sx={{ py: 4 }}
        >
          {renderPage()}
        </Container>
      </Box>
    </>
  );
}

export default App;