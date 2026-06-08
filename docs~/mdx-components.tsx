import defaultComponents from 'fumadocs-ui/mdx';
import { mdxComponents } from 'ach-fumadocs-theme';
import type { MDXComponents } from 'mdx/types';

export function getMDXComponents(components?: MDXComponents): MDXComponents {
  return {
    ...defaultComponents,
    ...mdxComponents,
    ...components,
  };
}
