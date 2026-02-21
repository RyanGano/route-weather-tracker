import { useState } from 'react';
import Container from 'react-bootstrap/Container';
import Navbar from 'react-bootstrap/Navbar';
import Badge from 'react-bootstrap/Badge';
import Button from 'react-bootstrap/Button';
import Offcanvas from 'react-bootstrap/Offcanvas';
import ListGroup from 'react-bootstrap/ListGroup';

interface Route {
  id: string;
  origin: string;
  destination: string;
  highway: string;
  available: boolean;
}

const ROUTES: Route[] = [
  { id: 'stanwood-kalispell', origin: 'Stanwood, WA', destination: 'Kalispell, MT', highway: 'I-90', available: true },
  { id: 'seattle-spokane',    origin: 'Seattle, WA',  destination: 'Spokane, WA',    highway: 'I-90', available: false },
];

const activeRoute = ROUTES[0];

export default function RouteHeader() {
  const [showDrawer, setShowDrawer] = useState(false);

  return (
    <>
      {/* sticky-top keeps header visible while scrolling */}
      <Navbar bg="dark" data-bs-theme="dark" className="shadow sticky-top mb-4" style={{ zIndex: 1030 }}>
        <Container>
          <Navbar.Brand className="d-flex align-items-center gap-2 fw-bold">
            <span aria-hidden>&#127956;</span>
            Route Weather Tracker
          </Navbar.Brand>

          <div className="d-flex align-items-center gap-2 ms-auto">
            {/* Inline route info — visible on md+ screens */}
            <span className="d-none d-md-flex align-items-center gap-2 text-white-50 small me-1">
              <span className="text-white fw-semibold">{activeRoute.origin}</span>
              <span>&#8594;</span>
              <span className="text-white fw-semibold">{activeRoute.destination}</span>
              <Badge bg="info">{activeRoute.highway}</Badge>
            </span>

            {/* Route picker button — always visible */}
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

      <Offcanvas show={showDrawer} onHide={() => setShowDrawer(false)} placement="end">
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>Choose Route</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body>
          <ListGroup className="mb-3">
            {ROUTES.map((route) => (
              <ListGroup.Item
                key={route.id}
                action={route.available}
                active={route.id === activeRoute.id}
                disabled={!route.available}
                className="d-flex justify-content-between align-items-center"
              >
                <div>
                  <div className="fw-semibold">
                    {route.origin} &rarr; {route.destination}
                  </div>
                  <div className="text-muted small">{route.highway}</div>
                </div>
                {route.id === activeRoute.id
                  ? <Badge bg="primary">Active</Badge>
                  : <Badge bg="secondary">Coming soon</Badge>}
              </ListGroup.Item>
            ))}
          </ListGroup>
          <p className="text-muted small">
            All routes track I-90 mountain passes with live webcam images,
            weather forecasts, and road conditions.
          </p>
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}
