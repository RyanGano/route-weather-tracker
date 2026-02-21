import Container from 'react-bootstrap/Container';
import Navbar from 'react-bootstrap/Navbar';
import Badge from 'react-bootstrap/Badge';

export default function RouteHeader() {
  return (
    <Navbar bg="dark" data-bs-theme="dark" className="mb-4 shadow-sm">
      <Container>
        <Navbar.Brand className="d-flex align-items-center gap-2 fs-5">
          <span>&#127956;</span>
          <span>Route Weather Tracker</span>
        </Navbar.Brand>
        <Navbar.Text className="d-flex align-items-center gap-2">
          <span className="text-white fw-semibold">Stanwood, WA</span>
          <span className="text-secondary">&#8594;</span>
          <span className="text-white fw-semibold">Kalispell, MT</span>
          <Badge bg="info" className="ms-2">I-90</Badge>
        </Navbar.Text>
      </Container>
    </Navbar>
  );
}
