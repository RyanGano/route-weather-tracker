import { useEffect, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import Container from "react-bootstrap/Container";
import Spinner from "react-bootstrap/Spinner";
import Alert from "react-bootstrap/Alert";
import Placeholder from "react-bootstrap/Placeholder";
import Card from "react-bootstrap/Card";
import RouteHeader from "./components/RouteHeader";
import RouteStatus from "./components/RouteStatus";
import PassCard from "./components/PassCard";
import { RefreshProvider } from "./contexts/RefreshContext";
import {
  getRouteEndpoints,
  getPassesByIds,
  computeRoutes,
} from "./services/passService";
import type { PassSummary } from "./types/passTypes";
import type { ComputedRoute, RouteEndpoint } from "./types/routeTypes";
import { routeToSlug, routeMatchesSlug } from "./utils/formatters";
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
  const { fromId, toId, routeSlug } = useParams<{
    fromId: string;
    toId: string;
    routeSlug: string;
  }>();
  const navigate = useNavigate();

  const [endpoints, setEndpoints] = useState<RouteEndpoint[]>([]);
  const [selectedFrom, setSelectedFrom] = useState<RouteEndpoint | null>(null);
  const [selectedTo, setSelectedTo] = useState<RouteEndpoint | null>(null);
  const [selectedRoute, setSelectedRoute] = useState<ComputedRoute | null>(
    null,
  );
  const [userPos, setUserPos] = useState<{ lat: number; lon: number } | null>(
    null,
  );
  const [passes, setPasses] = useState<PassSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Track whether we've already resolved the URL-encoded route so we don't
  // re-run the deep-link effect after the user manually picks a new route.
  const urlResolved = useRef(false);

  // Load endpoints once on mount
  useEffect(() => {
    getRouteEndpoints()
      .then(setEndpoints)
      .catch(() => {
        // Non-fatal — UI will show empty comboboxes
      });
  }, []);

  // Request geolocation once when the app first loads so comboboxes can sort
  useEffect(() => {
    if (!navigator.geolocation) return;
    let cancelled = false;
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        if (cancelled) return;
        setUserPos({ lat: pos.coords.latitude, lon: pos.coords.longitude });
      },
      () => {
        /* ignore errors silently; comboboxes will fallback to alpha order */
      },
      { maximumAge: 60_000, timeout: 7000 },
    );
    return () => {
      cancelled = true;
    };
  }, []);

  // When navigating directly to a permalink, auto-compute and select the route
  // once endpoints are available.
  useEffect(() => {
    if (urlResolved.current) return;
    if (!fromId || !toId || !routeSlug) return;
    if (endpoints.length === 0) return;

    const from = endpoints.find((e) => e.id === fromId);
    const to = endpoints.find((e) => e.id === toId);
    if (!from || !to) return;

    urlResolved.current = true;

    computeRoutes(fromId, toId)
      .then((routes) => {
        const match =
          routes.find((r) => routeMatchesSlug(r, routeSlug)) ?? routes[0];
        if (match) {
          setSelectedFrom(from);
          setSelectedTo(to);
          setSelectedRoute(match);
          setPasses([]);
        }
      })
      .catch(() => {
        // Non-fatal — user can still pick a route manually
      });
  }, [endpoints, fromId, toId, routeSlug]);

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
    urlResolved.current = true;
    navigate(`/${from.id}/${to.id}/${routeToSlug(route)}`, { replace: false });
  }


  return (
    <RefreshProvider>
      <RouteHeader
        endpoints={endpoints}
        selectedFrom={selectedFrom}
        selectedTo={selectedTo}
        selectedRoute={selectedRoute}
        userPos={userPos}
        onRouteChange={handleRouteChange}
      />
      <Container>
        {!selectedRoute && !loading && !(fromId && toId && routeSlug) && (
          <Alert variant="info" className="mt-2">
            Use the <strong>Route</strong> button above to choose your start and
            end city.
          </Alert>
        )}
        {fromId &&
          toId &&
          routeSlug &&
          !selectedRoute &&
          !loading &&
          endpoints.length > 0 && (
            <>
              <div className="d-flex align-items-center gap-2 mb-3 text-muted">
                <Spinner animation="border" size="sm" />
                <span>Loading route and pass conditions…</span>
              </div>
              <PassCardSkeleton />
            </>
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
