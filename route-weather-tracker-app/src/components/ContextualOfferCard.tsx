import { useState } from "react";
import { Link } from "react-router-dom";
import type { AffiliateOffer } from "../types/adTypes";

const DISMISS_KEY = "ad_offer_dismissed";

interface Props {
  offer: AffiliateOffer;
}

export default function ContextualOfferCard({ offer }: Props) {
  const [dismissed, setDismissed] = useState(
    () => sessionStorage.getItem(DISMISS_KEY) === "1",
  );

  if (dismissed) return null;

  function handleDismiss() {
    sessionStorage.setItem(DISMISS_KEY, "1");
    setDismissed(true);
  }

  return (
    <div
      className="d-flex align-items-start gap-3 px-3 py-2 mb-4 rounded border bg-body-secondary"
      style={{ fontSize: "0.875rem" }}
      role="complementary"
      aria-label="Sponsored travel tip"
    >
      <span style={{ fontSize: "1.25rem", lineHeight: 1.4 }} aria-hidden>
        {offer.emoji}
      </span>
      <div className="flex-grow-1">
        <span className="fw-semibold">{offer.headline}</span>
        {offer.subtext && (
          <span className="text-muted ms-1">{offer.subtext}</span>
        )}
        {offer.provider === "amazon" && (
          <span className="text-muted ms-1">
            (As an Amazon Associate I earn from qualifying purchases.)
          </span>
        )}
        <span className="text-muted ms-1" style={{ fontSize: "0.75rem" }}>
          ·{" "}
          <Link to="/privacy" className="text-muted">
            Privacy Policy
          </Link>
        </span>
      </div>
      <a
        href={offer.url}
        target="_blank"
        rel="noopener noreferrer sponsored"
        className="btn btn-sm btn-outline-secondary flex-shrink-0"
      >
        Shop&nbsp;&#8599;
      </a>
      <button
        type="button"
        className="btn-close flex-shrink-0"
        style={{ fontSize: "0.65rem" }}
        aria-label="Dismiss"
        onClick={handleDismiss}
      />
    </div>
  );
}
