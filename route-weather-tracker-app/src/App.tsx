import { useEffect, useState } from "react";
import Container from "react-bootstrap/Container";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import Placeholder from "react-bootstrap/Placeholder";
import Card from "react-bootstrap/Card";
import RouteHeader from "./components/RouteHeader";
import RouteStatus from "./components/RouteStatus";
import PassCard from "./components/PassCard";
import { getRouteEndpoints, getAllPasses } from "./services/passService";
import type { PassSummary } from "./types/passTypes";
import type { RouteEndpoint, SelectedRoute } from "./types/routeTypes";
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
  const [selectedRoute, setSelectedRoute] = useState<SelectedRoute | null>(
    null,
  );
  const [passes, setPasses] = useState<PassSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load route endpoints once on mount and default to Stanwood → Kalispell
  useEffect(() => {
    getRouteEndpoints()
      .then((eps) => {
        setEndpoints(eps);
        const from = eps.find((e) => e.id === "stanwood");
        const to = eps.find((e) => e.id === "kalispell");
        if (from && to) setSelectedRoute({ from, to });
      })
      .catch(() => {
        // Non-fatal — the pass fetch will show the real error
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
        const data = await getAllPasses(
          selectedRoute!.from.id,
          selectedRoute!.to.id,
        );
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

  return (
    <>
      <RouteHeader
        endpoints={endpoints}
        selectedRoute={selectedRoute}
        onRouteChange={setSelectedRoute}
      />
      <Container>
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

        {!loading && !error && passes.length === 0 && (
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
    </>
  );
}
