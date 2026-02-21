import { useState, useEffect } from "react";
import Container from "react-bootstrap/Container";
import Navbar from "react-bootstrap/Navbar";
import Badge from "react-bootstrap/Badge";
import Button from "react-bootstrap/Button";
import Offcanvas from "react-bootstrap/Offcanvas";
import Form from "react-bootstrap/Form";
import ListGroup from "react-bootstrap/ListGroup";
import type { RouteEndpoint, SelectedRoute } from "../types/routeTypes";
import { passesOnRoute } from "../types/routeTypes";

interface Props {
  endpoints: RouteEndpoint[];
  selectedRoute: SelectedRoute | null;
  onRouteChange: (route: SelectedRoute) => void;
}

function endpointLabel(ep: RouteEndpoint) {
  return `${ep.name}, ${ep.state}`;
}

export default function RouteHeader({ endpoints, selectedRoute, onRouteChange }: Props) {
  const [showDrawer, setShowDrawer] = useState(false);

  // Draft state inside the drawer — committed only when the user clicks Apply
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
  const draftTo   = endpoints.find((e) => e.id === draftToId) ?? null;

  const previewPasses =
    draftFrom && draftTo && draftFrom.id !== draftTo.id
      ? passesOnRoute(draftFrom, draftTo)
      : [];

  const canApply =
    draftFrom !== null &&
    draftTo !== null &&
    draftFrom.id !== draftTo.id &&
    previewPasses.length > 0;

  function handleApply() {
    if (!draftFrom || !draftTo) return;
    onRouteChange({ from: draftFrom, to: draftTo });
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
                <Badge bg="info">I-90</Badge>
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
          <Form>
            <Form.Group className="mb-3" controlId="routeFrom">
              <Form.Label className="fw-semibold">From</Form.Label>
              <Form.Select
                value={draftFromId}
                onChange={(e) => setDraftFromId(e.target.value)}
                disabled={endpoints.length === 0}
              >
                <option value="">Select a starting point…</option>
                {endpoints.map((ep) => (
                  <option key={ep.id} value={ep.id}>
                    {endpointLabel(ep)}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>

            <Form.Group className="mb-4" controlId="routeTo">
              <Form.Label className="fw-semibold">To</Form.Label>
              <Form.Select
                value={draftToId}
                onChange={(e) => setDraftToId(e.target.value)}
                disabled={endpoints.length === 0}
              >
                <option value="">Select a destination…</option>
                {endpoints
                  .filter((ep) => ep.id !== draftFromId)
                  .map((ep) => (
                    <option key={ep.id} value={ep.id}>
                      {endpointLabel(ep)}
                    </option>
                  ))}
              </Form.Select>
            </Form.Group>
          </Form>

          {/* Live pass preview */}
          {draftFrom && draftTo && draftFrom.id !== draftTo.id && (
            <div className="mb-4">
              <p className="fw-semibold mb-2">
                {previewPasses.length > 0
                  ? `${previewPasses.length} pass${previewPasses.length > 1 ? "es" : ""} on this route:`
                  : "No tracked passes on this segment."}
              </p>
              {previewPasses.length > 0 && (
                <ListGroup variant="flush">
                  {previewPasses.map((p) => (
                    <ListGroup.Item
                      key={p.id}
                      className="px-0 py-1 d-flex justify-content-between align-items-center"
                    >
                      <span>{p.name}</span>
                      <Badge bg="secondary">{p.state}</Badge>
                    </ListGroup.Item>
                  ))}
                </ListGroup>
              )}
            </div>
          )}

          <div className="d-grid">
            <Button
              variant="primary"
              onClick={handleApply}
              disabled={!canApply}
            >
              Apply Route
            </Button>
          </div>

          <p className="text-muted small mt-3">
            All routes track I-90 mountain passes with live webcam images,
            weather forecasts, and road conditions.
          </p>
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}
