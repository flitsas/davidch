import NextLink from "next/link";
import type { ComponentProps } from "react";

export function FlitLink({
  className = "",
  ...props
}: ComponentProps<typeof NextLink>) {
  return (
    <NextLink
      className={[
        "font-medium text-flit-blue underline-offset-4 transition-colors duration-[var(--flit-duration-fast)] hover:text-flit-blue-dark hover:underline flit-focus-ring rounded-sm",
        className,
      ].join(" ")}
      {...props}
    />
  );
}
