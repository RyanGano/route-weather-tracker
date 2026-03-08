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
                <div className="mt-3 mb-4">
                  <section className="p-3 rounded shadow-sm" style={{ background: "linear-gradient(90deg,#e9f3ff,#f7fbff)" }}>
                    <h2 className="h4 mb-2">Plan safer mountain-pass trips</h2>
                    <p className="mb-2">
                      When to Drive surfaces the best time to cross mountain passes by
                      combining three key signals:
                    </p>
                    <ul className="mb-2">
                      <li>Live road condition updates from DOT agencies</li>
                      <li>Short-term weather forecasts focused on pass locations</li>
                      <li>Active restrictions and advisories that affect travel</li>
                    </ul>
                    <div className="d-flex gap-2 align-items-center">
                      <button
                        className="btn btn-primary btn-sm"
                        onClick={() =>
                          window.dispatchEvent(new CustomEvent("openInfoDrawer"))
                        }
                      >
                        Learn more
                      </button>
                      <small className="text-muted">Or use the <strong>Route</strong> button above to get started</small>
                    </div>
                  </section>
                </div>
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
          <>
            <Alert variant="info" className="mt-2">
              Use the <strong>Route</strong> button above to choose your start
              and end city.
            </Alert>

            {/* Inline info for SEO and first-time visitors. This content is
                visible on the page (not only in the drawer) so search engines
                and users landing on the root URL immediately see what the
                site does. */}
            <div className="mt-3 mb-4">
              <article aria-labelledby="about-heading">
                <h2 id="about-heading" className="h5 mb-2">
                  What is When to Drive?
                </h2>
                <p className="mb-1">
                  <strong>When to Drive</strong> helps you plan mountain pass
                  crossings by showing current road conditions, weather
                  forecasts, and active restrictions for every pass along your
                  route.
                </p>
                <p className="mb-1">
                  Select a starting city and a destination using the{" "}
                  <strong>Route</strong> button, and the app will identify which
                  passes you'll cross and surface the best window of time to
                  make the drive.
                </p>
                <p className="text-muted small mb-0">
                  Data is sourced from state DOT agencies and OpenWeather. This
                  information is for planning only — always verify with official
                  agency sources before traveling.
                </p>
              </article>
            </div>
          </>
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
