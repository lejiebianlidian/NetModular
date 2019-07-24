import module from '../module'
/** 路由数组 */
let routes = []

/** 面包屑 */
const breadcrumb = [
  {
    title: '首页',
    path: '/'
  },
  {
    title: module.name
  }
]

const requireComponent = require.context('../views', true, /\page.js$/)
requireComponent.keys().map(fileName => {
  const route = requireComponent(fileName).route
  routes.push({
    path: route.page.path,
    name: route.page.name,
    component: route.component,
    meta: {
      title: route.page.title,
      frameIn: route.page.frameIn,
      cache: route.page.cache,
      breadcrumb,
      buttons: route.page.buttons
    }
  })
})

export default routes
