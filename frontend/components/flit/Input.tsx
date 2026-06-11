import type { InputHTMLAttributes, ReactNode } from "react";

type FlitInputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  error?: string | null;
  hint?: string;
  icon?: ReactNode;
};

export function FlitInput({
  label,
  error,
  hint,
  icon,
  id,
  className = "",
  ...props
}: FlitInputProps) {
  const inputId = id ?? props.name;

  return (
    <div className="space-y-2">
      <label htmlFor={inputId} className="block text-sm font-semibold text-flit-text-primary">
        {label}
      </label>
      <div className="relative">
        {icon && (
          <span
            className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-flit-blue"
            aria-hidden="true"
          >
            {icon}
          </span>
        )}
        <input
          id={inputId}
          className={[
            "flit-focus-ring h-12 w-full rounded-[10px] border border-flit-border-input bg-flit-bg-card px-4 text-base text-flit-text-primary placeholder:text-flit-text-muted transition-colors duration-[var(--flit-duration-fast)]",
            icon ? "pl-11" : "",
            error ? "border-flit-danger" : "",
            className,
          ]
            .filter(Boolean)
            .join(" ")}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
          {...props}
        />
      </div>
      {hint && !error && (
        <p id={`${inputId}-hint`} className="text-sm text-flit-text-secondary">
          {hint}
        </p>
      )}
      {error && (
        <p id={`${inputId}-error`} role="alert" className="text-sm text-flit-danger">
          {error}
        </p>
      )}
    </div>
  );
}

export function PasswordInput(props: Omit<FlitInputProps, "type">) {
  return <FlitInput type="password" {...props} />;
}
