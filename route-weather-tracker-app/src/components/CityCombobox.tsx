import { useState, useEffect, useRef, useId } from "react";
import Form from "react-bootstrap/Form";
import type { RouteEndpoint } from "../types/routeTypes";
import { endpointLabel } from "../utils/formatters";

interface Props {
  label: string;
  endpoints: RouteEndpoint[];
  value: string; // endpoint id
  onChange: (id: string) => void;
  disabled?: boolean;
  placeholder?: string;
  /** Endpoint id to exclude from the list (e.g. already-selected other end) */
  exclude?: string;
}

export default function CityCombobox({
  label,
  endpoints,
  value,
  onChange,
  disabled = false,
  placeholder = "Type to search…",
  exclude,
}: Props) {
  const inputId = useId();
  const listId = useId();
  const containerRef = useRef<HTMLDivElement>(null);

  // The text shown in the input field
  const [inputText, setInputText] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(-1);

  // Sync input text whenever the selected value changes externally
  useEffect(() => {
    const ep = endpoints.find((e) => e.id === value);
    setInputText(ep ? endpointLabel(ep) : "");
  }, [value, endpoints]);

  const candidates = endpoints
    .filter((ep) => {
      if (exclude && ep.id === exclude) return false;
      return endpointLabel(ep).toLowerCase().includes(inputText.toLowerCase());
    })
    .sort((a, b) => endpointLabel(a).localeCompare(endpointLabel(b)));

  function handleInputChange(text: string) {
    setInputText(text);
    setIsOpen(true);
    setHighlightedIndex(-1);
    // If the text is cleared, also clear the selection
    if (text === "") onChange("");
  }

  function selectEndpoint(ep: RouteEndpoint) {
    onChange(ep.id);
    setInputText(endpointLabel(ep));
    setIsOpen(false);
    setHighlightedIndex(-1);
  }

  function handleBlur(e: React.FocusEvent) {
    // Close only if focus leaves the whole container
    if (!containerRef.current?.contains(e.relatedTarget as Node | null)) {
      setIsOpen(false);
      // Revert text to the current confirmed selection (or blank if none)
      const ep = endpoints.find((e) => e.id === value);
      setInputText(ep ? endpointLabel(ep) : "");
    }
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (!isOpen) {
      if (e.key === "ArrowDown" || e.key === "Enter") {
        setIsOpen(true);
        setHighlightedIndex(0);
        e.preventDefault();
      }
      return;
    }

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setHighlightedIndex((i) => Math.min(i + 1, candidates.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlightedIndex((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (highlightedIndex >= 0 && candidates[highlightedIndex]) {
        selectEndpoint(candidates[highlightedIndex]);
      }
    } else if (e.key === "Escape") {
      setIsOpen(false);
      const ep = endpoints.find((e) => e.id === value);
      setInputText(ep ? endpointLabel(ep) : "");
    }
  }

  return (
    <Form.Group
      className="mb-3 position-relative"
      onBlur={handleBlur}
      ref={containerRef}
    >
      <Form.Label className="fw-semibold" htmlFor={inputId}>
        {label}
      </Form.Label>
      <Form.Control
        id={inputId}
        type="text"
        autoComplete="off"
        role="combobox"
        aria-expanded={isOpen}
        aria-controls={listId}
        aria-autocomplete="list"
        value={inputText}
        placeholder={disabled ? "" : placeholder}
        disabled={disabled}
        onChange={(e) => handleInputChange(e.target.value)}
        onFocus={(e) => {
          e.target.select();
          setIsOpen(true);
        }}
        onKeyDown={handleKeyDown}
      />

      {isOpen && !disabled && (
        <ul
          id={listId}
          role="listbox"
          className="list-unstyled mb-0 border rounded shadow-sm bg-body"
          style={{
            position: "absolute",
            top: "100%",
            left: 0,
            right: 0,
            zIndex: 1050,
            maxHeight: "12rem",
            overflowY: "auto",
          }}
        >
          {candidates.length === 0 ? (
            <li className="px-3 py-2 text-muted small">No matches</li>
          ) : (
            candidates.map((ep, i) => (
              <li
                key={ep.id}
                role="option"
                aria-selected={ep.id === value}
                className="px-3 py-2"
                style={{
                  cursor: "pointer",
                  background:
                    i === highlightedIndex
                      ? "var(--bs-primary)"
                      : ep.id === value
                        ? "var(--bs-secondary-bg)"
                        : undefined,
                  color: i === highlightedIndex ? "white" : undefined,
                }}
                onMouseDown={(e) => e.preventDefault()} // prevent blur before click
                onClick={() => selectEndpoint(ep)}
                onMouseEnter={() => setHighlightedIndex(i)}
                onMouseLeave={() => setHighlightedIndex(-1)}
              >
                {endpointLabel(ep)}
              </li>
            ))
          )}
        </ul>
      )}
    </Form.Group>
  );
}
