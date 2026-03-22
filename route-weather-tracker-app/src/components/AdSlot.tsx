import { useEffect, useRef } from "react";

interface Props {
  publisherId: string;
  adUnitId: string;
}

export default function AdSlot({ publisherId, adUnitId }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const pushed = useRef(false);

  useEffect(() => {
    if (pushed.current) return;
    pushed.current = true;

    // The AdSense script is loaded from index.html <head>.
    // Just push the ad unit config so AdSense fills the slot.
    try {
      (
        (window as { adsbygoogle?: unknown[] }).adsbygoogle ??
        ((window as { adsbygoogle?: unknown[] }).adsbygoogle = [])
      ).push({});
    } catch {
      // Script not yet loaded; AdSense will auto-fill once it is
    }
  }, [publisherId]);

  return (
    <div
      ref={containerRef}
      className="mb-4"
      aria-label="Advertisement"
      role="complementary"
    >
      <ins
        className="adsbygoogle"
        style={{ display: "block" }}
        data-ad-client={publisherId}
        data-ad-slot={adUnitId}
        data-ad-format="auto"
        data-full-width-responsive="true"
      />
    </div>
  );
}
