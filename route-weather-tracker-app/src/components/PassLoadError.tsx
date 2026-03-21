import { useState } from "react";
import Card from "react-bootstrap/Card";
import Button from "react-bootstrap/Button";

interface Props {
  passId: string;
  passName?: string;
  onRetry: () => Promise<void>;
}

export default function PassLoadError({ passId, passName, onRetry }: Props) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleRetry() {
    setError(null);
    setLoading(true);
    try {
      await onRetry();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load pass data.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card className="mb-4 shadow-sm border-danger">
      <Card.Header className="text-danger">{passName ?? passId}</Card.Header>
      <Card.Body>
        <p className="mb-2">
          There was an error getting data for {passName ?? passId}.
        </p>
        {error && <p className="text-danger small">{error}</p>}
        <div>
          <Button
            variant="outline-primary"
            size="sm"
            onClick={handleRetry}
            disabled={loading}
          >
            {loading ? "Trying…" : "Try again"}
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
}
