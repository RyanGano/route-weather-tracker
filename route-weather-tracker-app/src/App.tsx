import { useEffect, useState } from "react";
import Container from "react-bootstrap/Container";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import Placeholder from "react-bootstrap/Placeholder";
import Card from "react-bootstrap/Card";
import RouteHeader from "./components/RouteHeader";
import RouteStatus from "./components/RouteStatus";
import PassCard from "./components/PassCard";
import { RefreshProvider } from "./contexts/RefreshContext";
import { getRouteEndpoints, getPassesByIds } from "./services/passService";
import type { PassSummary } from "./types/passTypes";
import type { ComputedRoute, RouteEndpoint } from "./types/routeTypes";
import "./App.css";

function PassCardSkeleton() {
  return (
    <Card className="mb-4 shadow-sm">
      <Card.Header>
        <Placeholder as="div" animation="glow">
          <Placeholder xs={4} /> <Placeholder xs={2} /> <Placeholder xs={2} />
        </Placeholder>
      </Card.Header>
      <Card.Body>
        <Placeholder as="p" animation="glow">
          <Placeholder xs={12} />
          <Placeholder xs={10} />
          <Placeholder xs={8} />
        </Placeholder>
      </Card.Body>
    </Card>
  );
}

export default function App() {
  const [endpoints, setEndpoints] = useState<RouteEndpoint[]>([]);
  const [selectedFrom, setSelectedFrom] = useState<RouteEndpoint | null>(null);
  const [selectedTo, setSelectedTo] = useState<RouteEndpoint | null>(null);
  const [selectedRoute, setSelectedRoute] = useState<ComputedRoute | null>(null);
  const [passes, setPasses] = useState<PassSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load endpoints once on mount
  useEffect(() => {
    getRouteEndpoints()
      .then(setEndpoints)
      .catch(() => {
        // Non-fatal — UI will show empty comboboxes
      });
  }, []);

  // Fetch pass data whenever the selected route changes
  useEffect(() => {
    if (!selectedRoute) return;

    let cancelled = false;

    async function fetchPasses() {
      try {
        setLoading(true);
        setError(null);
        const data = await getPassesByIds(selectedRoute!.passIds);
        if (!cancelled) setPasses(data);
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : "Unable to load pass data. The backend service may be unavailable.",
          );
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    }

    void fetchPasses();
    return () => {
      cancelled = true;
    };
  }, [selectedRoute]);

  function handleRouteChange(
    from: RouteEndpoint,
    to: RouteEndpoint,
    route: ComputedRoute,
  ) {
    setSelectedFrom(from);
    setSelectedTo(to);
    setSelectedRoute(route);
    setPasses([]);
  }

  return (
    <RefreshProvider>
      <RouteHeader
        endpoints={endpoints}
        selectedFrom={selectedFrom}
        selectedTo={selectedTo}
        selectedRoute={selectedRoute}
        onRouteChange={handleRouteChange}
      />
      <Container>
        {!selectedRoute && !loading && (
          <Alert variant="info" className="mt-2">
            Use the <strong>Route</strong> button above to choose your start and
            end city.
          </Alert>
        )}
        {loading && (
          <>
            <div className="d-flex align-items-center gap-2 mb-3 text-muted">
              <Spinner animation="border" size="sm" />
              <span>Loading pass conditions…</span>
            </div>
            <PassCardSkeleton />
            <PassCardSkeleton />
            <PassCardSkeleton />
          </>
        )}

        {!loading && error && (
          <Alert variant="danger">
            <Alert.Heading>Unable to load pass data</Alert.Heading>
            <p>{error}</p>
          </Alert>
        )}

        {!loading && !error && selectedRoute && passes.length === 0 && (
          <Alert variant="warning">
            No pass data returned from the service.
          </Alert>
        )}

        {!loading && !error && (
          <>
            <RouteStatus passes={passes} />
            {passes.map((pass) => (
              <PassCard key={pass.info.id} pass={pass} />
            ))}
          </>
        )}
      </Container>
    </RefreshProvider>
  );
}
