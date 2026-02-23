import { useState, useEffect } from "react";
import Container from "react-bootstrap/Container";
import Navbar from "react-bootstrap/Navbar";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Offcanvas from "react-bootstrap/Offcanvas";
import Spinner from "react-bootstrap/Spinner";
import type { ComputedRoute, RouteEndpoint } from "../types/routeTypes";
import { endpointLabel } from "../utils/formatters";
import { computeRoutes } from "../services/passService";
import CityCombobox from "./CityCombobox";

interface Props {
  endpoints: RouteEndpoint[];
  selectedFrom: RouteEndpoint | null;
  selectedTo: RouteEndpoint | null;
  selectedRoute: ComputedRoute | null;
  onRouteChange: (
    from: RouteEndpoint,
    to: RouteEndpoint,
    route: ComputedRoute,
  ) => void;
}

/** "I-90" → "Interstate 90", "US-2" → "US Highway 2", others pass through. */
function formatRouteName(name: string): string {
  return name
    .split(" / ")
    .map((h) => {
      if (h.startsWith("I-")) return `Interstate ${h.slice(2)}`;
      if (h.startsWith("US-")) return `US Highway ${h.slice(3)}`;
      return h;
    })
    .join(" / ");
}

export default function RouteHeader({
  endpoints,
  selectedFrom,
  selectedTo,
  selectedRoute,
  onRouteChange,
}: Props) {
  const [showDrawer, setShowDrawer] = useState(false);
  const [draftFromId, setDraftFromId] = useState("");
  const [draftToId, setDraftToId] = useState("");
  const [fetchedRoutes, setFetchedRoutes] = useState<ComputedRoute[]>([]);
  const [computing, setComputing] = useState(false);

  // Sync drafts when the drawer opens
  useEffect(() => {
    if (showDrawer) {
      setDraftFromId(selectedFrom?.id ?? "");
      setDraftToId(selectedTo?.id ?? "");
    }
  }, [showDrawer, selectedFrom, selectedTo]);

  // Re-compute routes whenever the city pair changes
  useEffect(() => {
    if (!draftFromId || !draftToId || draftFromId === draftToId) {
      setFetchedRoutes([]);
      return;
    }
    let cancelled = false;
    setComputing(true);
    computeRoutes(draftFromId, draftToId)
      .then((routes) => {
        if (!cancelled) setFetchedRoutes(routes);
      })
      .catch(() => {
        if (!cancelled) setFetchedRoutes([]);
      })
      .finally(() => {
        if (!cancelled) setComputing(false);
      });
    return () => {
      cancelled = true;
    };
  }, [draftFromId, draftToId]);

  const draftFrom = endpoints.find((e) => e.id === draftFromId) ?? null;
  const draftTo = endpoints.find((e) => e.id === draftToId) ?? null;

  // Routes ≤20% longer than the fastest are "reasonable" primary options.
  // Anything beyond that is grouped under a separate "Longer options" section
  // so they're visible but clearly distinguished (e.g. Spokane→Tacoma via US-2
  // adds ~60 miles and goes to the longer section; Spokane→Everett via US-2 is
  // only ~3% longer and stays in the primary section).
  const primaryDist = fetchedRoutes[0]?.distanceMiles ?? 0;
  const primaryRoutes = fetchedRoutes.filter(
    (r) => r.extraDistanceMiles == null || r.extraDistanceMiles <= primaryDist * 0.2,
  );
  const longerRoutes = fetchedRoutes.filter(
    (r) => r.extraDistanceMiles != null && r.extraDistanceMiles > primaryDist * 0.2,
  );

  function handleSelectRoute(route: ComputedRoute) {
    if (!draftFrom || !draftTo) return;
    onRouteChange(draftFrom, draftTo, route);
    setShowDrawer(false);
  }

  return (
    <>
      <Navbar
        bg="dark"
        data-bs-theme="dark"
        className="shadow sticky-top mb-4"
        style={{ zIndex: 1030 }}
      >
        <Container>
          <Navbar.Brand className="d-flex align-items-center gap-2 fw-bold">
            <span aria-hidden>&#127956;</span>
            When to Drive
          </Navbar.Brand>

          <div className="d-flex align-items-center gap-2 ms-auto">
            {selectedFrom && selectedTo && (
              <span className="d-none d-md-flex align-items-center gap-2 text-white-50 small me-1">
                <span className="text-white fw-semibold">
                  {endpointLabel(selectedFrom)}
                </span>
                <span>&#8594;</span>
                <span className="text-white fw-semibold">
                  {endpointLabel(selectedTo)}
                </span>
                {selectedRoute && (
                  <Badge bg="info">{selectedRoute.name}</Badge>
                )}
              </span>
            )}

            <Button
              variant="outline-light"
              size="sm"
              onClick={() => setShowDrawer(true)}
              aria-label="Choose route"
            >
              <span aria-hidden>&#128506;</span>
              <span className="ms-1 d-none d-sm-inline">Route</span>
            </Button>
          </div>
        </Container>
      </Navbar>

      <Offcanvas
        show={showDrawer}
        onHide={() => setShowDrawer(false)}
        placement="end"
      >
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>Choose Route</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body>
          <CityCombobox
            label="From"
            endpoints={endpoints}
            value={draftFromId}
            onChange={setDraftFromId}
            disabled={endpoints.length === 0}
            placeholder="Type a city or state…"
            exclude={draftToId}
          />
          <CityCombobox
            label="To"
            endpoints={endpoints}
            value={draftToId}
            onChange={setDraftToId}
            disabled={endpoints.length === 0}
            placeholder="Type a city or state…"
            exclude={draftFromId}
          />

          {draftFrom && draftTo && draftFrom.id !== draftTo.id && (
            <div className="mt-3">
              {computing ? (
                <div className="d-flex align-items-center gap-2 text-muted py-2">
                  <Spinner animation="border" size="sm" />
                  <span className="small">Finding routes…</span>
                </div>
              ) : fetchedRoutes.length === 0 ? (
                <p className="text-muted small mb-0">
                  No routes found between these cities.
                </p>
              ) : (
                <>
                  <div className="d-grid gap-2">
                    {primaryRoutes.map((route) => (
                      <Button
                        key={route.id}
                        variant="outline-primary"
                        className="text-start py-2 px-3"
                        onClick={() => handleSelectRoute(route)}
                      >
                        <div className="d-flex align-items-center gap-2">
                          <span className="fw-semibold">
                            {formatRouteName(route.name)}
                          </span>
                          {route.highwaysUsed.map((h) => (
                            <Badge key={h} bg="info">
                              {h}
                            </Badge>
                          ))}
                        </div>
                        <div className="text-muted small mt-1">
                          {route.passNames.length > 0
                            ? route.passNames.join(" • ")
                            : "No tracked passes"}
                          <span className="text-body-tertiary ms-2">
                            {Math.round(route.distanceMiles)} mi
                          </span>
                        </div>
                      </Button>
                    ))}
                  </div>

                  {longerRoutes.length > 0 && (
                    <div className="mt-3">
                      <p
                        className="text-muted mb-2 fw-semibold text-uppercase"
                        style={{ fontSize: "0.7rem", letterSpacing: "0.05em" }}
                      >
                        Longer options
                      </p>
                      <div className="d-grid gap-2">
                        {longerRoutes.map((route) => (
                          <Button
                            key={route.id}
                            variant="outline-secondary"
                            className="text-start py-2 px-3"
                            onClick={() => handleSelectRoute(route)}
                          >
                            <div className="d-flex align-items-center gap-2">
                              <span className="fw-semibold">
                                {formatRouteName(route.name)}
                              </span>
                              {route.highwaysUsed.map((h) => (
                                <Badge key={h} bg="secondary">
                                  {h}
                                </Badge>
                              ))}
                              <Badge
                                bg="warning"
                                text="dark"
                                className="ms-auto"
                              >
                                +{Math.round(route.extraDistanceMiles!)} mi
                              </Badge>
                            </div>
                            <div className="text-muted small mt-1">
                              {route.passNames.length > 0
                                ? route.passNames.join(" • ")
                                : "No tracked passes"}
                              <span className="text-body-tertiary ms-2">
                                {Math.round(route.distanceMiles)} mi total
                              </span>
                            </div>
                          </Button>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}
