import { useState, useEffect } from "react";
import Container from "react-bootstrap/Container";
import Navbar from "react-bootstrap/Navbar";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Offcanvas from "react-bootstrap/Offcanvas";
import type { Route, RouteEndpoint, SelectedRoute, PassWaypoint } from "../types/routeTypes";
import { passesOnRoute } from "../types/routeTypes";
import { endpointLabel } from "../utils/formatters";
import CityCombobox from "./CityCombobox";

interface Props {
  endpoints: RouteEndpoint[];
  routes: Route[];
  waypoints: PassWaypoint[];
  selectedRoute: SelectedRoute | null;
  onRouteChange: (route: SelectedRoute) => void;
}

export default function RouteHeader({
  endpoints,
  routes,
  waypoints,
  selectedRoute,
  onRouteChange,
}: Props) {
  const [showDrawer, setShowDrawer] = useState(false);
  const [draftFromId, setDraftFromId] = useState<string>("");
  const [draftToId, setDraftToId] = useState<string>("");

  // Sync drafts when the drawer opens
  useEffect(() => {
    if (showDrawer && selectedRoute) {
      setDraftFromId(selectedRoute.from.id);
      setDraftToId(selectedRoute.to.id);
    }
  }, [showDrawer, selectedRoute]);

  const draftFrom = endpoints.find((e) => e.id === draftFromId) ?? null;
  const draftTo = endpoints.find((e) => e.id === draftToId) ?? null;

  // Routes that have ≥1 pass between the chosen endpoints — used to render the option buttons.
  const routeOptions =
    draftFrom && draftTo && draftFrom.id !== draftTo.id
      ? routes
          .map((r) => ({
            route: r,
            passes: passesOnRoute(draftFrom, draftTo, r.highway, waypoints),
          }))
          .filter(({ passes }) => passes.length > 0)
      : [];

  function handleSelectRoute(r: Route) {
    if (!draftFrom || !draftTo) return;
    onRouteChange({ from: draftFrom, to: draftTo, highway: r.highway });
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
            Route Weather Tracker
          </Navbar.Brand>

          <div className="d-flex align-items-center gap-2 ms-auto">
            {/* Inline route summary — visible on md+ */}
            {selectedRoute && (
              <span className="d-none d-md-flex align-items-center gap-2 text-white-50 small me-1">
                <span className="text-white fw-semibold">
                  {endpointLabel(selectedRoute.from)}
                </span>
                <span>&#8594;</span>
                <span className="text-white fw-semibold">
                  {endpointLabel(selectedRoute.to)}
                </span>
                <Badge bg="info">{selectedRoute.highway}</Badge>
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

          {/* Route option buttons — shown once a valid pair is selected */}
          {draftFrom && draftTo && draftFrom.id !== draftTo.id && (
            <div className="d-grid gap-2">
              {routeOptions.length === 0 ? (
                <p className="text-muted small mb-0">
                  No tracked passes found on any route between these cities.
                </p>
              ) : (
                routeOptions.map(({ route, passes }) => (
                  <Button
                    key={route.id}
                    variant="outline-primary"
                    className="text-start py-2 px-3"
                    onClick={() => handleSelectRoute(route)}
                  >
                    <div className="d-flex align-items-center gap-2">
                      <span className="fw-semibold">{route.name}</span>
                      <Badge bg="info">{route.highway}</Badge>
                    </div>
                    <div className="text-muted small mt-1">
                      {passes.map((p) => p.name).join(" • ")}
                    </div>
                  </Button>
                ))
              )}
            </div>
          )}
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}
