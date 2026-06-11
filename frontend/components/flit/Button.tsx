import type { ButtonHTMLAttributes, ReactNode } from "react";

type ButtonVariant = "primary" | "success" | "navy" | "danger" | "ghost";

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "flit-gradient-primary text-flit-text-inverse shadow-flit-button hover:brightness-105 active:brightness-95",
  success:
    "flit-gradient-success text-flit-text-inverse shadow-flit-button hover:brightness-105 active:brightness-95",
  navy: "bg-flit-blue-dark text-flit-text-inverse hover:bg-flit-blue-dark/90 active:bg-flit-blue-dark/80",
  danger:
    "flit-gradient-danger text-flit-text-inverse shadow-flit-button hover:brightness-105 active:brightness-95",
  ghost:
    "border border-flit-border-soft bg-flit-bg-card text-flit-text-primary hover:bg-flit-bg-modal active:bg-flit-border-soft",
};

type FlitButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  fullWidth?: boolean;
  children: ReactNode;
};

export function FlitButton({
  variant = "primary",
  fullWidth = false,
  className = "",
  children,
  ...props
}: FlitButtonProps) {
  return (
    <button
      type="button"
      className={[
        "flit-focus-ring inline-flex min-h-11 items-center justify-center rounded-flit-pill px-8 text-base font-semibold transition-[filter,background-color,opacity] duration-[var(--flit-duration-base)] ease-[var(--flit-easing)] disabled:cursor-not-allowed disabled:opacity-50 motion-reduce:transition-none",
        variantClasses[variant],
        fullWidth ? "w-full" : "",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
      {...props}
    >
      {children}
    </button>
  );
}

export function GradientButton(props: Omit<FlitButtonProps, "variant">) {
  return <FlitButton variant="primary" {...props} />;
}

export function NavyButton(props: Omit<FlitButtonProps, "variant">) {
  return <FlitButton variant="navy" {...props} />;
}

export function DangerButton(props: Omit<FlitButtonProps, "variant">) {
  return <FlitButton variant="danger" {...props} />;
}
