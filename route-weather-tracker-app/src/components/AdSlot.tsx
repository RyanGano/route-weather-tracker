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

    if (!publisherId) return;

    const win = window as unknown as { adsbygoogle?: unknown[] };

    function pushAd() {
      try {
        (win.adsbygoogle ?? (win.adsbygoogle = [])).push({});
      } catch {}
    }

    // If the AdSense script is already present, just push the config.
    if ((window as any).adsbygoogle) {
      pushAd();
      return;
    }

    // Dynamically inject the AdSense script so we only load it when an ad
    // slot is actually shown (i.e. after the user selects a route).
    const script = document.createElement("script");
    script.async = true;
    script.crossOrigin = "anonymous";
    script.src = `https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=${publisherId}`;
    script.onload = () => {
      pushAd();
    };
    document.head.appendChild(script);
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
